using BPFTP.Models;
using BPFTP.Utils;
using Newtonsoft.Json.Linq;
using R3;
using Renci.SshNet;
using Renci.SshNet.Sftp;
using Serilog;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Threading;
using System.Threading.Tasks;

namespace BPFTP.Services;

public readonly record struct TransferProgress(ulong BytesTransfered, double Percentage, double Speed);
public readonly record struct DirectoryTransferProgress(string CurrentFile, double Percentage, double Speed);

public class SftpService(ISecureCredentialService secureCredentialService) : IDisposable
{
    private readonly ISecureCredentialService _secureCredentialService = secureCredentialService;
    private SftpClient? _client;

    public int WaitTimeForUpload = 0;
    public int WaitTimeForDownload = 0;
    public bool IsConnected => _client?.IsConnected ?? false;

    private ConnectionProfile _profile;
    SemaphoreSlim SemaphoreSlim = new(5);

    public async Task Connect2Async(ConnectionProfile profile)
    {
        if (_client?.IsConnected == true) _client.Disconnect();
        _client = (await ConnectAsync(profile)).Value;
        _profile = profile;
    }
    public async Task<DisposeWrapper<SftpClient>> ConnectAsync(ConnectionProfile profile, CancellationToken cancellationToken = default)
    {
        AuthenticationMethod authMethod;

        if (profile.AuthMethod == AuthroizeMethod.SSH && !string.IsNullOrEmpty(profile.PrivateKeyPath))
        {
            var keyPassword = await _secureCredentialService!.RetrieveCredentialAsync($"profile-{profile.Id}-keypassword") ?? profile.PrivateKeyPassword;
            var keyFile = new PrivateKeyFile(profile.PrivateKeyPath, keyPassword ?? string.Empty);
            authMethod = new PrivateKeyAuthenticationMethod(profile.Username, keyFile);
        }
        else
        {
            var password = await _secureCredentialService!.RetrieveCredentialAsync($"profile-{profile.Id}-password") ?? profile.Password;
            authMethod = new PasswordAuthenticationMethod(profile.Username, password ?? string.Empty);
        }

        var connectionInfo = new ConnectionInfo(profile.Host, profile.Port, profile.Username, authMethod);
        var theClient = new SftpClient(connectionInfo);
        await theClient.ConnectAsync(cancellationToken);

        return new DisposeWrapper<SftpClient> { Value = theClient, OnDispose = () => _ = 1 };
    }

    public IAsyncEnumerable<ISftpFile>? ListDirectoryAsync(string path,CancellationToken cancellationToken = default) => 
        _client?.ListDirectoryAsync(path, cancellationToken);

    public async Task<(Observable<TransferProgress> progressObservable, string fileName, ulong totalBytes)>
        DownloadFileObservable(string remotePath, string localPath,CancellationToken token = default)
    {

        var thisClient = await ConnectAsync(_profile, token);
        if (token.IsCancellationRequested)
        {
            return (Observable.Create<TransferProgress>(async (observer, cancellationToken) => { }), string.Empty, 0);
        }
        var fileInfo = await Task.Run(() => thisClient.Value.Get(remotePath));
        var localFileInfo = new FileInfo(localPath);
        long totalSize = fileInfo.Length;
        double percentMultiply = 100 /(double)totalSize;
        var fileName = fileInfo.Name;
        var uploadObservable = Observable.Create<ulong>(async (observer, cancellationToken) =>
        {
            if (!IsConnected || _client == null)
            {
                observer.OnErrorResume(new InvalidOperationException("Not connected"));
                return;
            }
            try
            {
                bool completed = false;
                await using var localStream = localFileInfo.OpenWrite();
                
                token.Register(() => {
                    if (completed) return;
                    thisClient.Value.Disconnect();
                    thisClient.Dispose();
                    localStream.Dispose();
                });
                long totalSize = fileInfo.Length;

                thisClient.Value.DownloadFile(remotePath,localStream ,
                    x => observer.OnNext(x));
                observer.OnCompleted();
                completed = true;
            }
            catch (Exception ex)
            {
                observer.OnErrorResume(ex);
            }
            finally
            {
                thisClient.Dispose();
            }
        });
        return (uploadObservable
            .ThrottleLast(TimeSpan.FromMilliseconds(50))
            .Timestamp()
            .Chunk(2, 1)
            .Where(buf => buf.Length >= 2)
            .Select(buf =>
            {
                var (Timestamp, Value) = buf[0];
                var curr = buf[^1];
                var MBDiff = (curr.Value - Value).ToMB();
                var secondsDiff = (curr.Timestamp - Timestamp).ToSecond();
                var speedBps = secondsDiff > 0 ? MBDiff / secondsDiff : 0;
                return new TransferProgress()
                {
                    Percentage = curr.Value * percentMultiply,
                    BytesTransfered = curr.Value,
                    Speed = speedBps
                };
            }), fileName, (ulong)totalSize);
    }

    public Observable<DirectoryTransferProgress> DownloadDirectoryObservable(string remotePath, string localPath)
    {
        return Observable.Create<DirectoryTransferProgress>(async (observer, cancellationToken) =>
        {
            using var thisClient = await ConnectAsync(_profile, cancellationToken);
            if (!IsConnected || _client == null)
            {
                observer.OnErrorResume(new InvalidOperationException("Not connected"));
            }
            try
            {
                await DownloadDirectoryRecursiveAsync(remotePath, localPath, observer, thisClient.Value, cancellationToken);
                observer.OnCompleted();
            }
            catch (OperationCanceledException)
            {
                observer.OnCompleted();
            }
            catch (Exception ex)
            {
                observer.OnErrorResume(ex);
            }
        });
    }

    private async Task DownloadDirectoryRecursiveAsync(string remoteDirPath, string localDirPath, Observer<DirectoryTransferProgress> observer,SftpClient sftpClient,CancellationToken token = default)
    {
        if (sftpClient == null) return;
        Directory.CreateDirectory(localDirPath);
        var items = await Task.Run(() => sftpClient.ListDirectory(remoteDirPath));

        foreach (var item in items)
        {
            if (item.Name == "." || item.Name == "..") continue;

            string remoteItemPath = item.FullName;
            string localItemPath = Path.Combine(localDirPath, item.Name);


            if (item.IsDirectory)
            {
                await DownloadDirectoryRecursiveAsync(remoteItemPath, localItemPath, observer, sftpClient, token);
            }
            else
            {

                var (progressObservable, fileName, totalBytes) = await DownloadFileObservable(remoteItemPath, localItemPath, token);
                observer.OnNext(new DirectoryTransferProgress(item.Name, 0, 0));
                await progressObservable.ForEachAsync(progress =>
                {
                    observer.OnNext(new DirectoryTransferProgress(item.Name, progress.Percentage, progress.Speed));
                }, cancellationToken: token);
            }
        }
    }

    public async Task<(Observable<TransferProgress> progressObservable,string fileName,ulong totalBytes)>
        UploadFileObservable(string localPath, string remotePath,CancellationToken token = default)
    {
        var thisClient = await ConnectAsync(_profile,token);
        if (token.IsCancellationRequested)
        {
            return (Observable.Create<TransferProgress>(async (observer, cancellationToken) => { } ),string.Empty,0 );
        }
        var fileInfo = new FileInfo(localPath);
        long totalSize = fileInfo.Length;
        var fileName = fileInfo.Name;
        double percentMultiply = 100 / (double)totalSize;
        var uploadObservable = Observable.Create<ulong>(async (observer, cancellationToken) =>
        {
            if (!IsConnected || _client == null)
            {
                observer.OnErrorResume(new InvalidOperationException("Not connected"));
                return;
            }
            try
            {
                bool completed = false;
                await using var localStream = fileInfo.OpenRead();
                token.Register(() => {
                    if (completed) return;
                    thisClient.Value.Disconnect();
                    localStream.Dispose();
                });
                thisClient.Value.UploadFile(localStream, remotePath,
                    x => observer.OnNext(x));
                observer.OnNext((ulong)totalSize);
                observer.OnCompleted();
                completed = true;
            }
            catch (Exception ex)
            {
                observer.OnErrorResume(ex);
            }
            finally
            {
                thisClient.Dispose();
            }
        });
        return (uploadObservable
            .ThrottleLast(TimeSpan.FromMilliseconds(50))
            .Timestamp()
            .Chunk(2, 1)
            .Where(buf => buf.Length >= 2)
            .Select(buf =>
            {
                var (Timestamp, Value) = buf[0];
                var curr = buf[^1];
                var MBDiff = (curr.Value - Value).ToMB();
                var secondsDiff = (curr.Timestamp - Timestamp).ToSecond();
                var speedBps = secondsDiff > 0 ? MBDiff / secondsDiff : 0;
                return new TransferProgress()
                {
                    Percentage = curr.Value * percentMultiply,
                    BytesTransfered = curr.Value,
                    Speed = speedBps
                };
            }), fileName, (ulong)totalSize);
    }

    public Observable<DirectoryTransferProgress> UploadDirectoryObservable(string localPath, string remotePath)
    {
        return Observable.Create<DirectoryTransferProgress>(async (observer, ct) =>
        {
            if (!IsConnected || _client == null)
            {
                observer.OnErrorResume(new InvalidOperationException("Not connected"));
                return;
            }
            using var thisClient = await ConnectAsync(_profile, ct);
            try
            {
                await UploadDirectoryRecursiveAsync(localPath, remotePath, observer, thisClient.Value, ct);
                observer.OnCompleted();
            }
            catch (Exception ex)
            {
                observer.OnErrorResume(ex);
            }
        });
    }

    private async Task UploadDirectoryRecursiveAsync(string localDirPath, string remoteDirPath, Observer<DirectoryTransferProgress> observer,SftpClient sftpClient,CancellationToken token = default)
    {
        if (_client == null) return;
        // Create the remote directory if it doesn't exist
        try
        {
            if (!sftpClient.Exists(remoteDirPath))
            {
                sftpClient.CreateDirectory(remoteDirPath);
            }
        }
        catch (Exception ex)
        {
            sftpClient.CreateDirectory(remoteDirPath);
        }

        var localDirInfo = new DirectoryInfo(localDirPath);

        // Upload subdirectories
        foreach (var dir in localDirInfo.GetDirectories())
        {
            string remoteSubDirPath = remoteDirPath + "/" + dir.Name;
            await UploadDirectoryRecursiveAsync(dir.FullName, remoteSubDirPath, observer, sftpClient, token);
        }

        // Upload files in the current directory
        var files = localDirInfo.GetFiles();
        foreach (var file in localDirInfo.GetFiles())
        {
            string remoteFilePath = remoteDirPath + "/" + file.Name;
            var (progressObservable, fileName, totalBytes) = await UploadFileObservable(file.FullName, remoteFilePath,token);
            observer.OnNext(new DirectoryTransferProgress(fileName, 0, 0));
            await progressObservable.ForEachAsync(progress =>
            {
                double percentage = totalBytes > 0 ? (double)progress.BytesTransfered / totalBytes * 100 : 0;
                observer.OnNext(new DirectoryTransferProgress(file.Name, percentage, progress.Speed));
            }, cancellationToken: token);
        }
    }


    public void Disconnect() => _client?.Disconnect();

    public void Dispose()
    {
        _client?.Dispose();
        GC.SuppressFinalize(this);
    }
}
