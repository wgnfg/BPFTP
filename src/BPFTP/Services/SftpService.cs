using BPFTP.Models;
using R3;
using Renci.SshNet;
using Renci.SshNet.Sftp;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BPFTP.Services;

public readonly record struct DownloadProgress(long BytesDownloaded, long TotalBytes);
public readonly record struct UploadProgress(long BytesUploaded, long TotalBytes);
public readonly record struct DirectoryDownloadProgress(string CurrentFile, double Percentage);
public readonly record struct DirectoryUploadProgress(string CurrentFile, double Percentage);

public class SftpService : IDisposable
{
    private SftpClient? _client;

    public bool IsConnected => _client?.IsConnected ?? false;

    public async Task ConnectAsync(ConnectionProfile profile)
    {
        if (_client?.IsConnected == true) _client.Disconnect();

        AuthenticationMethod authMethod;
        if (profile.AuthMethod ==  AuthroizeMethod.SSH && !string.IsNullOrEmpty(profile.PrivateKeyPath))
        {
            var keyPassword = profile.PrivateKeyPassword;
            var keyFile = new PrivateKeyFile(profile.PrivateKeyPath, keyPassword ?? string.Empty);
            authMethod = new PrivateKeyAuthenticationMethod(profile.Username, keyFile);
        }
        else
        {
            var password = profile.Password;
            authMethod = new PasswordAuthenticationMethod(profile.Username, password ?? string.Empty);
        }

        var connectionInfo = new ConnectionInfo(profile.Host, profile.Port, profile.Username, authMethod);
        _client = new SftpClient(connectionInfo);
        await _client.ConnectAsync(default);
    }

    public IAsyncEnumerable<ISftpFile> ListDirectoryAsync(string path,CancellationToken cancellationToken = default) => 
        _client?.ListDirectoryAsync(path, cancellationToken);

    public Observable<DownloadProgress> DownloadFileObservable(string remotePath, string localPath)
    {
        return Observable.Create<DownloadProgress>(async (observer, cancellationToken) =>
        {
            if (!IsConnected || _client == null)
            {
                observer.OnErrorResume(new InvalidOperationException("Not connected"));
                return;
            }

            byte[]? buffer = null;
            try
            {
                var fileInfo = await Task.Run(() => _client.Get(remotePath), cancellationToken);
                long totalSize = fileInfo.Length;
                long totalDownloaded = 0;

                buffer = ArrayPool<byte>.Shared.Rent(81920);
                var memory = buffer.AsMemory();

                await using var remoteStream = _client.OpenRead(remotePath);
                await using var localStream = new FileStream(localPath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, true);

                int bytesRead;
                while ((bytesRead = await remoteStream.ReadAsync(memory, cancellationToken)) > 0)
                {
                    await localStream.WriteAsync(memory[..bytesRead], cancellationToken);
                    totalDownloaded += bytesRead;
                    observer.OnNext(new DownloadProgress(totalDownloaded, totalSize));
                    await Task.Delay(1000);
                }

                await localStream.FlushAsync(cancellationToken);
                await localStream.DisposeAsync();
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

            if (!IsConnected || _client == null)
            {
                observer.OnErrorResume(new InvalidOperationException("Not connected"));
            }
            try
            {
                await DownloadDirectoryRecursiveAsync(remotePath, localPath, observer);
                observer.OnCompleted();
            }
            catch (Exception ex)
            {
                observer.OnErrorResume(ex);
            }
        });
    }

    private async Task DownloadDirectoryRecursiveAsync(string remoteDirPath, string localDirPath, Observer<DirectoryDownloadProgress> observer)
    {
        if (_client == null) return;
        Directory.CreateDirectory(localDirPath);
        var items = await Task.Run(() => _client.ListDirectory(remoteDirPath));

        foreach (var item in items)
        {
            if (item.Name == "." || item.Name == "..") continue;

            string remoteItemPath = item.FullName;
            string localItemPath = Path.Combine(localDirPath, item.Name);

            if (item.IsDirectory)
            {
                await DownloadDirectoryRecursiveAsync(remoteItemPath, localItemPath, observer);
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

            byte[]? buffer = null;
            try
            {
                var fileInfo = new FileInfo(localPath);
                long totalSize = fileInfo.Length;

                await using var localStream = fileInfo.OpenRead();
                await using var remoteStream = _client.OpenWrite(remotePath);

                long totalUploaded = 0;
                buffer = ArrayPool<byte>.Shared.Rent(81920);
                var memory = buffer.AsMemory();
                int bytesRead;

                while ((bytesRead = await localStream.ReadAsync(memory, cancellationToken)) > 0)
                {
                    await remoteStream.WriteAsync(memory[..bytesRead], cancellationToken);
                    totalUploaded += bytesRead;
                    observer.OnNext(new UploadProgress(totalUploaded, totalSize));
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

            try
            {
                await UploadDirectoryRecursiveAsync(localPath, remotePath, observer).ConfigureAwait(false);
                observer.OnCompleted();
            }
            catch (Exception ex)
            {
                observer.OnErrorResume(ex);
            }
        });
    }

    private async Task UploadDirectoryRecursiveAsync(string localDirPath, string remoteDirPath, Observer<DirectoryUploadProgress> observer)
    {
        if (_client == null) return;

        // Create the remote directory if it doesn't exist
        try
        {
            if (!_client.Exists(remoteDirPath))
            {
                _client.CreateDirectory(remoteDirPath);
            }
        }
        catch (Exception ex)
        {
            _client.CreateDirectory(remoteDirPath);
        }

        var localDirInfo = new DirectoryInfo(localDirPath);

        // Upload subdirectories
        foreach (var dir in localDirInfo.GetDirectories())
        {
            string remoteSubDirPath = remoteDirPath + "/" + dir.Name;
            await UploadDirectoryRecursiveAsync(dir.FullName, remoteSubDirPath, observer);
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
