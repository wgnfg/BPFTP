using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using BPFTP.Services;

namespace BPFTP.ViewModels
{
    public partial class SftpWorkspaceViewModel : ViewModelBase
    {
        public SftpWorkspaceViewModel(DatabaseService databaseService, SftpService sftpService, IViewService viewService, FileService fileService, IPermissionService permissionService, ISecureCredentialService secureCredentialService)
        {
            _databaseService = databaseService;
            _sftpService = sftpService;
            _viewService = viewService;
            _fileService = fileService;
            _permissionService = permissionService;
            _secureCredentialService = secureCredentialService;
            _ = InitConnectionItemsAsync();
            InitExplorer();
        }
        private readonly DatabaseService _databaseService;
        private readonly SftpService _sftpService;
        private readonly IViewService _viewService;
        private readonly FileService _fileService;
        private readonly IPermissionService _permissionService;
        private readonly ISecureCredentialService _secureCredentialService;
        private bool ConnectionSelected() => SelectedConnection != null;
        [RelayCommand(CanExecute = nameof(ConnectionSelected))]
        private async Task Connect()
        {
            if (SelectedConnection == null) return;
            try
            {
                await _sftpService.Connect2Async(SelectedConnection);
                RemoteExplorer.CurPath = "/";
                ViewOperation.ShowPopupShort(new NormalPopupViewModel() { Message = $"连接成功" });
            }
            catch (Exception ex)
            {
                ViewOperation.ShowPopupShort(new NormalPopupViewModel() { Message = $"连接失败:{ex.Message}" });
            }
        }

    }
}
