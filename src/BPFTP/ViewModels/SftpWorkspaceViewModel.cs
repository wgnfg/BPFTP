using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using BPFTP.Services;

namespace BPFTP.ViewModels
{
    public partial class SftpWorkspaceViewModel : ViewModelBase
    {
        public SftpWorkspaceViewModel(DatabaseService databaseService, SftpService sftpService,ViewService viewService)
        {
            _databaseService = databaseService;
            _sftpService = sftpService;
            _viewService = viewService;
            _ = InitializeAsync();
        }
        private readonly DatabaseService _databaseService;
        private readonly SftpService _sftpService;
        private readonly ViewService _viewService;

        private bool ConnectionSelected() => SelectedConnection != null;
        [RelayCommand(CanExecute = nameof(ConnectionSelected))]
        private async Task Connect()
        {
            if (SelectedConnection == null) return;
            try
            {
                await _sftpService.ConnectAsync(SelectedConnection);
            }
            catch (Exception ex)
            {
            }
        }

    }
}
