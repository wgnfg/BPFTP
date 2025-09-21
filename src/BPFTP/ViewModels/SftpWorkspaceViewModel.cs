using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using BPFTP.Services;

namespace BPFTP.ViewModels
{
    public partial class SftpWorkspaceViewModel : ViewModelBase
    {
        public SftpWorkspaceViewModel(DatabaseService databaseService, SftpService sftpService)
        {
            _databaseService = databaseService;
            _sftpService = sftpService;
            _ = InitializeAsync();
        }
        private readonly DatabaseService _databaseService;
        private readonly SftpService _sftpService;

        private bool CanExecuteConnectionAction() => SelectedConnection != null;
        [RelayCommand(CanExecute = nameof(CanExecuteConnectionAction))]
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
