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
        [NotifyCanExecuteChangedFor(nameof(SshCommand))]
        private ConnectionProfile? _selectedConnection;

        [RelayCommand]
        private async Task AddConnection()
        {
            var theConnectionProfile = new ConnectionProfile();
            await ViewOperation.ShowDialogAsync(new EditConnectionViewModel(theConnectionProfile, OnConfirm: async () =>
            {
                if (theConnectionProfile != null)
                {
                    await SaveOrUpdateConnection(theConnectionProfile);
                }
                ViewOperation.ShowPopupShort(new NormalPopupViewModel{ Message = "添加成功"});
            }));
        }

        [RelayCommand(CanExecute = nameof(ConnectionSelected))]
        private async Task EditConnection()
        {
            if (SelectedConnection == null) return;
            var profileToEdit = SelectedConnection.Clone();
            var vm = new EditConnectionViewModel(profileToEdit);
            await ViewOperation.ShowDialogAsync(new EditConnectionViewModel(profileToEdit, OnConfirm: async () =>
            {
                if (profileToEdit != null)
                {
                    await SaveOrUpdateConnection(profileToEdit);
                }
                ViewOperation.ShowPopupShort(new NormalPopupViewModel { Message = "修改成功" });
            }));
        }

        private async Task SaveOrUpdateConnection(ConnectionProfile profile)
        {
            // Store credentials in local variables to be used after the profile is saved.
            var password = profile.Password;
            var privateKeyPassword = profile.PrivateKeyPassword;

            profile.Password = string.Empty;
            profile.PrivateKeyPassword = string.Empty;

            await _databaseService.SaveConnectionAsync(profile);

            if (profile.Id > 0)
            {
                if (!string.IsNullOrEmpty(password))
                {
                    await _secureCredentialService.StoreCredentialAsync($"profile-{profile.Id}-password", password);
                }

                if (!string.IsNullOrEmpty(privateKeyPassword))
                {
                    await _secureCredentialService.StoreCredentialAsync($"profile-{profile.Id}-keypassword", privateKeyPassword);
                }
            }

            await InitConnectionItemsAsync();
        }

        private async Task InitConnectionItemsAsync()
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
                await InitConnectionItemsAsync();
            }
        }
    }
}
