using BPFTP.Services;
using CommunityToolkit.Mvvm.Input;
using R3;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BPFTP.ViewModels
{
    public partial class SftpWorkspaceViewModel
    {
        [RelayCommand]
        public async Task UploadItem(FileItemViewModel? item)
        {
            if (item == null || !_sftpService.IsConnected) return;

            if (item.IsDirectory)
            {
                var progressVm = new TransferProgressViewModel { Title = $"上传: {item.Name}" };
                ViewService.Instance.ShowPopup(progressVm, 0);
                await UploadDirectoryAsync(item, progress =>
                {
                    progressVm.CurrentFileName = progress.CurrentFile;
                    progressVm.Percentage = progress.Percentage;
                });
                ViewService.Instance.HidePopup(progressVm);
            }
            else
            {
                var progressVm = new TransferProgressViewModel { Title = $"上传: {item.Name}", CurrentFileName = item.Name };
                ViewService.Instance.ShowPopup(progressVm);
                await UploadFileAsync(item,
                    progress => progressVm.Percentage = progress.TotalBytes > 0 ? (double)progress.BytesUploaded / progress.TotalBytes * 100 : 0);
                ViewService.Instance.HidePopup(progressVm);
            }

            await RemoteExplorer.Refresh();
        }

        [RelayCommand]
        public async Task DownloadItem(FileItemViewModel? item)
        {
            if (item == null || !_sftpService.IsConnected) return;

            var localPath = Path.Combine(LocalExplorer.CurPath, item.Name);

            if (item.IsDirectory)
            {
                var progressVm = new TransferProgressViewModel { Title = $"下载文件夹: {item.Name}" };
                ViewService.Instance.ShowPopup(progressVm);
                await DownloadDirectoryAsync(item, localPath, progress =>
                {
                    progressVm.CurrentFileName = progress.Name;
                    progressVm.Percentage = progress.Percentage;
                });
                ViewService.Instance.HidePopup(progressVm);
            }
            else
            {
                var progressVm = new TransferProgressViewModel { CurrentFileName = item.Name };
                ViewService.Instance.ShowPopup(progressVm);
                await DownloadFileAsync(item, localPath, progress => progressVm.Percentage = progress.Percentage);
                ViewService.Instance.HidePopup(progressVm);
            }

            await LocalExplorer.Refresh();
        }

        [RelayCommand]
        private async Task UploadMany()
        {
            List<FileItemViewModel> selectedItems = [.. LocalExplorer.AllFolders.Where(i => i.IsSelected)
                ,..LocalExplorer.AllFolders.Where(i => i.IsSelected)];

            if (selectedItems.Count == 0 || !_sftpService.IsConnected) return;

            var progressVm = new TransferProgressViewModel { Title = "批量上传" };
            ViewService.Instance.ShowPopup(progressVm);

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
                        });
                    }
                    else
                    {
                        progressVm.Title = $"{progressPrefix}Uploading File: {item.Name}";
                        progressVm.CurrentFileName = item.Name;
                        await UploadFileAsync(item, progress =>
                        {
                            progressVm.Percentage = progress.TotalBytes > 0 ? (double)progress.BytesUploaded / progress.TotalBytes * 100 : 0;
                        });
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
                ViewService.Instance.HidePopup(progressVm);
                await RemoteExplorer.Refresh();
            }
        }

        [RelayCommand]
        private async Task DownloadMany()
        {
            List<FileItemViewModel> selectedItems = [..RemoteExplorer.AllFolders.Where(i => i.IsSelected)
                ,..RemoteExplorer.AllFiles.Where(i => i.IsSelected)];

            if (!selectedItems.Any() || !_sftpService.IsConnected) return;

            var progressVm = new TransferProgressViewModel { Title = "Batch Downloading..." };
            ViewService.Instance.ShowPopup(progressVm);

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
                ViewService.Instance.HidePopup(progressVm);
                await LocalExplorer.Refresh();
            }
        }


        private async Task UploadFileAsync(FileItemViewModel item, Action<UploadProgress> onProgress)
        {
            var remotePath = Path.Combine(RemoteExplorer.CurPath, item.Name).Replace('\\', '/');
            await _sftpService.UploadFileObservable(item.Path, remotePath)
                .ThrottleLast(TimeSpan.FromMilliseconds(200))
                .ForEachAsync(onProgress);
        }

        private async Task UploadDirectoryAsync(FileItemViewModel item, Action<DirectoryUploadProgress> onProgress)
        {
            var remotePath = Path.Combine(RemoteExplorer.CurPath, item.Name).Replace('\\', '/');
            await _sftpService.UploadDirectoryObservable(item.Path, remotePath)
                .ThrottleLast(TimeSpan.FromMilliseconds(200))
                .ForEachAsync(onProgress);
        }

        private async Task DownloadFileAsync(FileItemViewModel item, string localPath, Action<DownloadProgress> onProgress)
        {
            await _sftpService.DownloadFileObservable(item.Path, localPath)
                .ThrottleLast(TimeSpan.FromMilliseconds(200))
                .ForEachAsync(progress =>
                    onProgress(new DownloadProgress { Percentage = progress.TotalBytes > 0 ? (double)progress.BytesDownloaded / progress.TotalBytes * 100 : 0 }));
        }

        private async Task DownloadDirectoryAsync(FileItemViewModel item, string localPath, Action<DownloadProgress> onProgress)
        {
            await _sftpService.DownloadDirectoryObservable(item.Path, localPath)
                .ThrottleLast(TimeSpan.FromMilliseconds(200))
                .ForEachAsync(progress => onProgress(new() { Name = progress.CurrentFile, Percentage = progress.Percentage }));
        }

    }
}
