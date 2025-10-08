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

            // Store credentials securely first.
            await StoreCredentialsAsync(profile);

            // Clear credentials from the profile object before saving to the database.
            profile.Password = string.Empty;
            profile.PrivateKeyPassword = string.Empty;

            await _databaseService.SaveConnectionAsync(profile);
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

        private async Task StoreCredentialsAsync(ConnectionProfile profile)
        {
            // The profile must have an ID to create a unique key.
            // If it's a new profile, SaveConnectionAsync should assign one.
            if (profile.Id == 0)
            {
                await _databaseService.SaveConnectionAsync(profile);
            }

            if (!string.IsNullOrEmpty(profile.Password))
            {
                await _secureCredentialService.StoreCredentialAsync($"profile-{profile.Id}-password", profile.Password);
            }

            if (!string.IsNullOrEmpty(profile.PrivateKeyPassword))
            {
                await _secureCredentialService.StoreCredentialAsync($"profile-{profile.Id}-keypassword", profile.PrivateKeyPassword);
            }
        }   
    }
}
