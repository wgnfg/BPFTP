using BPFTP.Models;
using BPFTP.Utils;
using R3;
using Renci.SshNet;
using Renci.SshNet.Sftp;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Threading;
using System.Threading.Tasks;

namespace BPFTP.Services;

public readonly record struct DownloadProgress(long BytesDownloaded, long TotalBytes);
public readonly record struct UploadProgress(long BytesUploaded, long TotalBytes);
public readonly record struct DirectoryDownloadProgress(string CurrentFile, double Percentage);
public readonly record struct DirectoryUploadProgress(string CurrentFile, double Percentage);

public class SftpService(ISecureCredentialService secureCredentialService) : IDisposable
{
    private readonly ISecureCredentialService _secureCredentialService = secureCredentialService;
    private SftpClient? _client;

    public int WaitTimeForUpload = 1000;
    public int WaitTimeForDownload = 1000;
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

    public Observable<DownloadProgress> DownloadFileObservable(string remotePath, string localPath)
    {
        return Observable.Create<DownloadProgress>(async (observer, cancellationToken) =>
        {
            if (!IsConnected || _client == null || _profile == null)
            {
                observer.OnErrorResume(new InvalidOperationException("Not connected"));
                return;
            }
            using var thisClient = await ConnectAsync(_profile, cancellationToken);
            byte[]? buffer = null;
            try
            {
                var fileInfo = await Task.Run(() => thisClient.Value.Get(remotePath), cancellationToken);
                long totalSize = fileInfo.Length;
                long totalDownloaded = 0;

                buffer = ArrayPool<byte>.Shared.Rent(81920);
                var memory = buffer.AsMemory();

                await using var remoteStream = thisClient.Value.OpenRead(remotePath);
                await using var localStream = new FileStream(localPath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, true);

                int bytesRead;
                while ((bytesRead = await remoteStream.ReadAsync(memory, cancellationToken)) > 0)
                {
                    await localStream.WriteAsync(memory[..bytesRead], cancellationToken);
                    totalDownloaded += bytesRead;
                    observer.OnNext(new DownloadProgress(totalDownloaded, totalSize));
                    await Task.Delay(WaitTimeForDownload, cancellationToken);
                }

                await localStream.FlushAsync(cancellationToken);
                await localStream.DisposeAsync();
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
            finally
            {
                if (buffer != null)
                {
                    ArrayPool<byte>.Shared.Return(buffer);
                }
            }
        });
    }

    public Observable<DirectoryDownloadProgress> DownloadDirectoryObservable(string remotePath, string localPath)
    {
        return Observable.Create<DirectoryDownloadProgress>(async (observer, cancellationToken) =>
        {
            using var thisClient = await ConnectAsync(_profile);
            if (!IsConnected || _client == null)
            {
                observer.OnErrorResume(new InvalidOperationException("Not connected"));
            }
            try
            {
                await DownloadDirectoryRecursiveAsync(remotePath, localPath, observer, thisClient.Value);
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

    private async Task DownloadDirectoryRecursiveAsync(string remoteDirPath, string localDirPath, Observer<DirectoryDownloadProgress> observer,SftpClient sftpClient)
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
                await DownloadDirectoryRecursiveAsync(remoteItemPath, localItemPath, observer, sftpClient);
            }
            else
            {
                await foreach (var progress in DownloadFileObservable(remoteItemPath, localItemPath).ToAsyncEnumerable())
                {
                    double percentage = progress.TotalBytes > 0 ? (double)progress.BytesDownloaded / progress.TotalBytes * 100 : 0;
                    observer.OnNext(new DirectoryDownloadProgress(item.Name, percentage));
                }
            }
        }
    }

    public Observable<UploadProgress> UploadFileObservable(string localPath, string remotePath)
    {
        return Observable.Create<UploadProgress>(async (observer, cancellationToken) =>
        {
            if (!IsConnected || _client == null)
            {
                observer.OnErrorResume(new InvalidOperationException("Not connected"));
                return;
            }
            using var thisClient = await ConnectAsync(_profile, cancellationToken);
            byte[]? buffer = null;
            try
            {
                var fileInfo = new FileInfo(localPath);
                long totalSize = fileInfo.Length;

                await using var localStream = fileInfo.OpenRead();
                await using var remoteStream = thisClient.Value.OpenWrite(remotePath);

                long totalUploaded = 0;
                buffer = ArrayPool<byte>.Shared.Rent(81920);
                var memory = buffer.AsMemory();
                int bytesRead;

                while ((bytesRead = await localStream.ReadAsync(memory, cancellationToken)) > 0)
                {
                    await remoteStream.WriteAsync(memory[..bytesRead], cancellationToken);
                    totalUploaded += bytesRead;
                    observer.OnNext(new UploadProgress(totalUploaded, totalSize));
                    await Task.Delay(WaitTimeForUpload, cancellationToken);
                }

                await remoteStream.FlushAsync(cancellationToken);
                await remoteStream.DisposeAsync();
                observer.OnCompleted();
            }
            catch (Exception ex)
            {
                observer.OnErrorResume(ex);
            }
            finally
            {
                if (buffer != null)
                {
                    ArrayPool<byte>.Shared.Return(buffer);
                }
            }
        });
    }

    public Observable<DirectoryUploadProgress> UploadDirectoryObservable(string localPath, string remotePath)
    {
        return Observable.Create<DirectoryUploadProgress>(async (observer, ct) =>
        {
            if (!IsConnected || _client == null)
            {
                observer.OnErrorResume(new InvalidOperationException("Not connected"));
                return;
            }
            using var thisClient = await ConnectAsync(_profile, ct);
            try
            {
                await UploadDirectoryRecursiveAsync(localPath, remotePath, observer, thisClient.Value).ConfigureAwait(false);
                observer.OnCompleted();
            }
            catch (Exception ex)
            {
                observer.OnErrorResume(ex);
            }
        });
    }

    private async Task UploadDirectoryRecursiveAsync(string localDirPath, string remoteDirPath, Observer<DirectoryUploadProgress> observer,SftpClient sftpClient)
    {
        if (_client == null) return;
        using var thisClient = await ConnectAsync(_profile);
        // Create the remote directory if it doesn't exist
        try
        {
            if (!thisClient.Value.Exists(remoteDirPath))
            {
                thisClient.Value.CreateDirectory(remoteDirPath);
            }
        }
        catch (Exception ex)
        {
            thisClient.Value.CreateDirectory(remoteDirPath);
        }

        var localDirInfo = new DirectoryInfo(localDirPath);

        // Upload subdirectories
        foreach (var dir in localDirInfo.GetDirectories())
        {
            string remoteSubDirPath = remoteDirPath + "/" + dir.Name;
            await UploadDirectoryRecursiveAsync(dir.FullName, remoteSubDirPath, observer, thisClient.Value);
        }

        // Upload files in the current directory
        foreach (var file in localDirInfo.GetFiles())
        {
            string remoteFilePath = remoteDirPath + "/" + file.Name;
            await foreach (var progress in UploadFileObservable(file.FullName, remoteFilePath).ToAsyncEnumerable())
            {
                double percentage = progress.TotalBytes > 0 ? (double)progress.BytesUploaded / progress.TotalBytes * 100 : 0;
                observer.OnNext(new DirectoryUploadProgress(file.Name, percentage));
            }
        }
    }


    public void Disconnect() => _client?.Disconnect();

    public void Dispose()
    {
        _client?.Dispose();
        GC.SuppressFinalize(this);
    }
}
