using BPFTP.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
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
        private ConnectionProfile? _selectedConnection;


        [RelayCommand]
        private async Task AddConnection()
        {
            await SaveOrUpdateConnection(new ConnectionProfile() { Name = "Connect2", Username="foo",Password="1234", AuthMethod= AuthroizeMethod.Password,Host="127.0.0.1" });
        }

        [RelayCommand]
        private async Task EditConnection()
        {
            if (SelectedConnection == null) return;

            await SaveOrUpdateConnection(SelectedConnection);
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

        [RelayCommand]
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
