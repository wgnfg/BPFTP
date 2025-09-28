using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using BPFTP.Services;

namespace BPFTP.ViewModels
{
    public partial class SftpWorkspaceViewModel : ViewModelBase
    {
        public SftpWorkspaceViewModel(DatabaseService databaseService, SftpService sftpService, ViewService viewService, FileService fileService)
        {
            _databaseService = databaseService;
            _sftpService = sftpService;
            _viewService = viewService;
            _fileService = fileService;
            _ = InitializeAsync();
            InitExplorer();
        }
        private readonly DatabaseService _databaseService;
        private readonly SftpService _sftpService;
        private readonly ViewService _viewService;
        private readonly FileService _fileService;

        private bool ConnectionSelected() => SelectedConnection != null;
        [RelayCommand(CanExecute = nameof(ConnectionSelected))]
        private async Task Connect()
        {
            if (SelectedConnection == null) return;
            try
            {
                await _sftpService.Connect2Async(SelectedConnection);
                RemoteExplorer.CurPath = "/";
                ViewService.ShowPopupShort(new NormalPopupViewModel() { Message = $"连接成功" });
            }
            catch (Exception ex)
            {
                ViewService.ShowPopupShort(new NormalPopupViewModel() { Message = $"连接失败:{ex.Message}" });
            }
        }

    }
}
