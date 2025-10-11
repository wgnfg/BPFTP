using BPFTP.Models;
using BPFTP.Services;
using CommunityToolkit.Mvvm.ComponentModel;
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
        public RemoteExplorer RemoteExplorer { get; private set; } = new RemoteExplorer();
        public LocalExplorer LocalExplorer { get; private set; } = new LocalExplorer();

        private async Task LoadRemoteDirectoryAsync(string path)
        {
            if (!_sftpService.IsConnected) return;
            try
            {
                List<FileItemViewModel> newFolders = [];
                List<FileItemViewModel> newFiles = [];
                IAsyncEnumerable<Renci.SshNet.Sftp.ISftpFile>? items = _sftpService.ListDirectoryAsync(path);
                if (items != null)
                {
                    await foreach (var item in items)
                    {
                        if (item.IsDirectory && item.Name != "." && item.Name != "..")
                        {
                            newFolders.Add(new RemoteFolder { Name = item.Name, Path = item.FullName, IsDirectory = true, Size = item.Length });
                        }
                        else if (!item.IsDirectory)
                        {
                            newFiles.Add(new RemoteFile { Name = item.Name, Path = item.FullName, IsDirectory = false, Size = item.Length });
                        }
                    }
                }
                RemoteExplorer.AllFolders = newFolders;
                RemoteExplorer.AllFiles = newFiles;
                RemoteExplorer.FilterView();
            }
            catch (Exception ex) {
                RemoteExplorer.AllFolders = [];
                RemoteExplorer.AllFiles = [];
                RemoteExplorer.FilterView();
            }
        }
        private async Task LoadLocalDirectoryAsync(string path)
        {
            if (_permissionService != null)
            {
                var granted = await _permissionService.RequestStoragePermissionAsync();
                if (!granted)
                {
                    ViewOperation.ShowPopupShort(new NormalPopupViewModel() { Message = "获取权限失败" });
                    return;
                }
            }

            var (directories, files) = await _fileService.ListDirectoryAsync(path);
            LocalExplorer.AllFolders = [.. directories.Select(d => new LocalFolder { Name = d.Name, Path = d.FullName, IsDirectory = true }).OrderBy(d => d.Name)];
            LocalExplorer.AllFiles = [.. files.Select(f => new LocalFile { Name = f.Name, Path = f.FullName, IsDirectory = false, Size = f.Length }).OrderBy(f => f.Name)];
            LocalExplorer.FilterView();
        }


        public void InitExplorer()
        {
            LocalExplorer = new LocalExplorer()
            {
                UpdateFoldersAndFiles = LoadLocalDirectoryAsync,
                CurPath = OperatingSystem.IsWindows() ? "D://" : OperatingSystem.IsAndroid() ? "/storage/emulated/0" : Environment.CurrentDirectory
            };
            RemoteExplorer = new RemoteExplorer()
            {
                UpdateFoldersAndFiles = LoadRemoteDirectoryAsync,
            };
        }
    }
}
