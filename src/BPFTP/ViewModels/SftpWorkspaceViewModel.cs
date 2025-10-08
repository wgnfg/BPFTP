using Avalonia.Xaml.Interactions.DragAndDrop;
using BPFTP.Handlers;
using BPFTP.Services;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Threading.Tasks;

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
            RemoteDropHandler = new RemoteDropHandler(this);
            LocalDropHandler = new LocalDropHandler(this);
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
        public IDropHandler RemoteDropHandler { get; }
        public IDropHandler LocalDropHandler { get; }
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
