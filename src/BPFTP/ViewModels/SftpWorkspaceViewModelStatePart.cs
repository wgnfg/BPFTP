
using BPFTP.Models;
using R3;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace BPFTP.ViewModels
{
    public partial class SftpWorkspaceViewModel
    {
        public void InitStateSaving()
        {
            Observable.EveryValueChanged(LocalExplorer, x => x.CurPath)
                .ThrottleLast(TimeSpan.FromSeconds(1))
                .Skip(2)
                .Subscribe(path => _databaseService.SaveSettingAsync(SettingKey.LastLocalPath, path));

            Observable.EveryValueChanged(LocalExplorer, x => x.SearchText)
                .ThrottleLast(TimeSpan.FromSeconds(1))
                .Skip(1)
                .Subscribe(path => _databaseService.SaveSettingAsync(SettingKey.LastLocalSearch, path));

            Observable.EveryValueChanged(RemoteExplorer, x => x.CurPath)
                .Skip(2)
                .ThrottleLast(TimeSpan.FromSeconds(1))
                .Subscribe(path => _databaseService.SaveSettingAsync(SettingKey.LastRemotePath, path));

            Observable.EveryValueChanged(RemoteExplorer, x => x.SearchText)
                .ThrottleLast(TimeSpan.FromSeconds(1))
                .Skip(1)
                .Subscribe(path => _databaseService.SaveSettingAsync(SettingKey.LastRemoteSearch, path));
        }

        partial void OnSelectedConnectionChanged(ConnectionProfile? value)
        {
            if (value != null)
            {
                _ = _databaseService.SaveSettingAsync(SettingKey.LastConnectionProfileId, value.Id.ToString());
            }
        }

        private async Task InitStateAsync()
        {
            await Task.Delay(100); 

            var lastConnectionIdStr = await _databaseService.GetSettingAsync(SettingKey.LastConnectionProfileId);
            if (int.TryParse(lastConnectionIdStr, out var lastConnectionId))
            {
                SelectedConnection = Connections.FirstOrDefault(c => c.Id == lastConnectionId);
            }

            var lastLocalPath = await _databaseService.GetSettingAsync(SettingKey.LastLocalPath);
            if (!string.IsNullOrEmpty(lastLocalPath))
            {
                LocalExplorer.CurPath = lastLocalPath;
            }

            var lastRemotePath = await _databaseService.GetSettingAsync(SettingKey.LastRemotePath);
            if (!string.IsNullOrEmpty(lastRemotePath))
            {
                RemoteExplorer.CurPath = lastRemotePath;
            }

            var lastLocalSearch = await _databaseService.GetSettingAsync(SettingKey.LastLocalSearch);
            LocalExplorer.SearchText = lastLocalSearch ?? string.Empty;


            var lastRemoteSearch = await _databaseService.GetSettingAsync(SettingKey.LastRemoteSearch);
            RemoteExplorer.SearchText = lastRemoteSearch ?? string.Empty;
        }
    }
}
