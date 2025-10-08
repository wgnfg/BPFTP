using BPFTP.Services;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using R3;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BPFTP.ViewModels
{
    public partial class SftpWorkspaceViewModel
    {
        [RelayCommand]
        public void UploadItem(FileItemViewModel? item)
        {
            _ = Task.Run(async () =>
            {
                if (item == null || !_sftpService.IsConnected) return;
                var progressVm = new TransferProgressViewModel { Title = $"上传: {item.Name}" };
                using (var scope = ViewOperation.PopupScope(progressVm))
                {
                    if (item.IsDirectory)
                    {
                        await UploadDirectoryAsync(item, progress =>
                        {
                            progressVm.CurrentFileName = progress.CurrentFile;
                            progressVm.Percentage = progress.Percentage;
                        }, progressVm.CancellationToken);
                    }
                    else
                    {
                        progressVm.CurrentFileName = item.Name;
                        await UploadFileAsync(item,
                            progress => progressVm.Percentage = progress.TotalBytes > 0 ? (double)progress.BytesUploaded / progress.TotalBytes * 100 : 0,
                            progressVm.CancellationToken);
                    }
                }
                await RemoteExplorer.Refresh();
            });
        }

        [RelayCommand]
        public void DownloadItem(FileItemViewModel? item)
        {
            if (item == null || !_sftpService.IsConnected) return;

            _ = Task.Run(async () =>
            {
                var localPath = Path.Combine(LocalExplorer.CurPath, item.Name);
                var progressVm = new TransferProgressViewModel { Title = $"下载: {item.Name}" };
                using var scope = ViewOperation.PopupScope(progressVm);
                if (item.IsDirectory)
                {
                    await DownloadDirectoryAsync(item, localPath, progress =>
                    {
                        progressVm.CurrentFileName = progress.Name;
                        progressVm.Percentage = progress.Percentage;
                    }, progressVm.CancellationToken);
                }
                else
                {
                    progressVm.CurrentFileName = item.Name;
                    await DownloadFileAsync(item, localPath, progress => progressVm.Percentage = progress.Percentage, progressVm.CancellationToken);
                }

                await LocalExplorer.Refresh();
            });
        }

        [RelayCommand]
        private void UploadMany()
        {
            _ = Task.Run(async () =>
            {
                List<FileItemViewModel> selectedItems = [.. LocalExplorer.AllFolders.Where(i => i.IsSelected)
                ,..LocalExplorer.AllFiles.Where(i => i.IsSelected)];

                if (selectedItems.Count == 0 || !_sftpService.IsConnected) return;

                var progressVm = new TransferProgressViewModel { Title = "批量上传" };
                using var scope = ViewOperation.PopupScope(progressVm);
                try
                {
                    var totalItems = selectedItems.Count;
                    var processedItems = 0;

                    foreach (var item in selectedItems)
                    {
                        processedItems++;
                        var progressPrefix = $"({processedItems}/{totalItems}) ";

                        if (item.IsDirectory)
                        {
                            progressVm.Title = $"{progressPrefix}Uploading Dir: {item.Name}";
                            await UploadDirectoryAsync(item, progress =>
                            {
                                progressVm.CurrentFileName = progress.CurrentFile;
                                progressVm.Percentage = progress.Percentage;
                            }, progressVm.CancellationToken);
                        }
                        else
                        {
                            progressVm.Title = $"{progressPrefix}Uploading File: {item.Name}";
                            progressVm.CurrentFileName = item.Name;
                            await UploadFileAsync(item, progress =>
                            {
                                progressVm.Percentage = progress.TotalBytes > 0 ? (double)progress.BytesUploaded / progress.TotalBytes * 100 : 0;
                            }, progressVm.CancellationToken);
                        }
                    }
                }
                catch (Exception ex)
                {
                    progressVm.ErrorMessage = $"Batch upload failed: {ex.Message}";
                    await Task.Delay(3000); // Show error for a bit
                }
                finally
                {
                    await RemoteExplorer.Refresh();
                }
            });
        }

        [RelayCommand]
        private void DownloadMany()
        {
            _ = Task.Run(async () =>
            {
                List<FileItemViewModel> selectedItems = [..RemoteExplorer.AllFolders.Where(i => i.IsSelected)
                ,..RemoteExplorer.AllFiles.Where(i => i.IsSelected)];

                if (!selectedItems.Any() || !_sftpService.IsConnected) return;

                var progressVm = new TransferProgressViewModel { Title = "Batch Downloading..." };
                using var _ = ViewOperation.PopupScope(progressVm);

                try
                {
                    var totalItems = selectedItems.Count;
                    var processedItems = 0;
                    progressVm.TotalFileCount = totalItems;
                    foreach (var item in selectedItems)
                    {
                        processedItems++;
                        progressVm.CurrentFileIndex = processedItems;
                        var progressPrefix = $"({processedItems}/{totalItems}) ";
                        var localPath = Path.Combine(LocalExplorer.CurPath, item.Name);

                        if (item.IsDirectory)
                        {
                            progressVm.Title = $"{progressPrefix}Downloading Dir: {item.Name}";
                            await DownloadDirectoryAsync(item, localPath, progress =>
                            {
                                progressVm.CurrentFileName = progress.Name;
                                progressVm.Percentage = progress.Percentage;
                            });
                        }
                        else
                        {
                            progressVm.Title = $"{progressPrefix}Downloading File: {item.Name}";
                            progressVm.CurrentFileName = item.Name;
                            await DownloadFileAsync(item, localPath, progress =>
                            {
                                progressVm.Percentage = progress.Percentage;
                            });
                        }
                    }
                }
                catch (Exception ex)
                {
                    progressVm.ErrorMessage = $"Batch download failed: {ex.Message}";
                    await Task.Delay(3000);
                }
                finally
                {
                    await LocalExplorer.Refresh();
                }
            });
        }


        private async Task UploadFileAsync(FileItemViewModel item, Action<UploadProgress> onProgress, CancellationToken cancellationToken = default)
        {
            var remotePath = Path.Combine(RemoteExplorer.CurPath, item.Name).Replace('\\', '/');
            _logger.LogInformation("上传文件 {LocalPath} 到 {RemotePath}", item.Path, remotePath);
            try
            {
                await _sftpService.UploadFileObservable(item.Path, remotePath)
                    .ThrottleLast(TimeSpan.FromMilliseconds(200))
                    .ForEachAsync(onProgress, cancellationToken);
                _logger.LogInformation("Successfully uploaded file {LocalPath} to {RemotePath}", item.Path, remotePath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to upload file {LocalPath} to {RemotePath}", item.Path, remotePath);
            }
        }

        private async Task UploadDirectoryAsync(FileItemViewModel item, Action<DirectoryUploadProgress> onProgress,CancellationToken cancellationToken = default)
        {
            var remotePath = Path.Combine(RemoteExplorer.CurPath, item.Name).Replace('\\', '/');
            _logger.LogInformation("Uploading directory {LocalPath} to {RemotePath}", item.Path, remotePath);
            try
            {
                await _sftpService.UploadDirectoryObservable(item.Path, remotePath)
                    .ThrottleLast(TimeSpan.FromMilliseconds(200))
                    .ForEachAsync(onProgress, cancellationToken);
                _logger.LogInformation("Successfully uploaded directory {LocalPath} to {RemotePath}", item.Path, remotePath);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failed to upload directory {LocalPath} to {RemotePath}", item.Path, remotePath);
            }
        }

        private async Task DownloadFileAsync(FileItemViewModel item, string localPath, Action<DownloadProgress> onProgress,CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Downloading file {RemotePath} to {LocalPath}", item.Path, localPath);
            try
            {
                await _sftpService.DownloadFileObservable(item.Path, localPath)
                    .ThrottleLast(TimeSpan.FromMilliseconds(20))
                    .ForEachAsync(progress =>
                        onProgress(new DownloadProgress { Percentage = progress.TotalBytes > 0 ? (double)progress.BytesDownloaded / progress.TotalBytes * 100 : 0 }),
                        cancellationToken);
                _logger.LogInformation("Successfully downloaded file {RemotePath} to {LocalPath}", item.Path, localPath);
            }
            catch (Exception ex) {
                _logger.LogError(ex, "Failed to download file {RemotePath} to {LocalPath}", item.Path, localPath);
                ViewOperation.ShowPopupShort(new NormalPopupViewModel() { Message = $"下载失败:{ex.Message}" });
            }
        }

        private async Task DownloadDirectoryAsync(FileItemViewModel item, string localPath, Action<DownloadProgress> onProgress, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Downloading directory {RemotePath} to {LocalPath}", item.Path, localPath);
            try
            {
                await _sftpService.DownloadDirectoryObservable(item.Path, localPath)
                    .ForEachAsync(progress => onProgress(new() { Name = progress.CurrentFile, Percentage = progress.Percentage }), cancellationToken);
                _logger.LogInformation("Successfully downloaded directory {RemotePath} to {LocalPath}", item.Path, localPath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to download directory {RemotePath} to {LocalPath}", item.Path, localPath);
            }
        }

    }
}
