using Avalonia.Xaml.Interactions.DragAndDrop;
using BPFTP.Handlers;
using BPFTP.Services;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace BPFTP.ViewModels
{
    public partial class SftpWorkspaceViewModel : ViewModelBase
    {
        public SftpWorkspaceViewModel(DatabaseService databaseService, SftpService sftpService, IViewService viewService, FileService fileService, IPermissionService permissionService, ISecureCredentialService secureCredentialService, ILogger<SftpWorkspaceViewModel> logger)
        {
            _databaseService = databaseService;
            _sftpService = sftpService;
            _viewService = viewService;
            _fileService = fileService;
            _permissionService = permissionService;
            _secureCredentialService = secureCredentialService;
            _logger = logger;
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
        private readonly ILogger<SftpWorkspaceViewModel> _logger;
        private bool ConnectionSelected() => SelectedConnection != null;
        public IDropHandler RemoteDropHandler { get; }
        public IDropHandler LocalDropHandler { get; }
        [RelayCommand(CanExecute = nameof(ConnectionSelected))]
        private async Task Connect()
        {
            if (SelectedConnection == null) return;
            _logger.LogInformation("Connecting to {Host}", SelectedConnection.Host);
            try
            {
                await _sftpService.Connect2Async(SelectedConnection);
                RemoteExplorer.CurPath = "/";
                _logger.LogInformation("Successfully connected to {Host}", SelectedConnection.Host);
                ViewOperation.ShowPopupShort(new NormalPopupViewModel() { Message = $"连接成功" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to connect to {Host}", SelectedConnection.Host);
                ViewOperation.ShowPopupShort(new NormalPopupViewModel() { Message = $"连接失败:{ex.Message}" });
            }
        }

    }
}
