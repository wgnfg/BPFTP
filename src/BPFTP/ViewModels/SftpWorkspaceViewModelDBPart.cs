using BPFTP.Models;
using BPFTP.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Renci.SshNet.Messages;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BPFTP.ViewModels
{
    public partial class SftpWorkspaceViewModel
    {

        [ObservableProperty]
        private ObservableCollection<ConnectionProfile> _connections = [];

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(ConnectCommand))]
        [NotifyCanExecuteChangedFor(nameof(EditConnectionCommand))]
        [NotifyCanExecuteChangedFor(nameof(DeleteConnectionCommand))]
        private ConnectionProfile? _selectedConnection;


        [RelayCommand]
        private async Task AddConnection()
        {
            var theConnectionProfile = new ConnectionProfile();
            await ViewService.ShowDialogAsync(new EditConnectionViewModel(theConnectionProfile, OnConfirm: async () =>
            {
                if (theConnectionProfile != null)
                {
                    await SaveOrUpdateConnection(theConnectionProfile);
                }
                ViewService.ShowPopupShort(new NormalPopupViewModel{ Message = "添加成功"});
            }));
        }

        [RelayCommand(CanExecute = nameof(ConnectionSelected))]
        private async Task EditConnection()
        {
            if (SelectedConnection == null) return;
            var profileToEdit = SelectedConnection.Clone();
            var vm = new EditConnectionViewModel(profileToEdit);
            await ViewService.ShowDialogAsync(new EditConnectionViewModel(profileToEdit, OnConfirm: async () =>
            {
                if (profileToEdit != null)
                {
                    await SaveOrUpdateConnection(profileToEdit);
                }
                ViewService.ShowPopupShort(new NormalPopupViewModel { Message = "修改成功" });
            }));
        }

        private async Task SaveOrUpdateConnection(ConnectionProfile profile)
        {
            await _databaseService.SaveConnectionAsync(profile);
            await InitializeAsync();
        }

        private async Task InitializeAsync()
        {
            var connections = await _databaseService.GetConnectionsAsync();
            Connections = new ObservableCollection<ConnectionProfile>(connections);
        }

        [RelayCommand(CanExecute = nameof(ConnectionSelected))]
        private async Task DeleteConnection()
        {
            if (SelectedConnection != null)
            {
                await _databaseService.DeleteConnectionAsync(SelectedConnection.Id);
                await InitializeAsync();
            }
        }
    }
}
