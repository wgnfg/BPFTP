using Avalonia.Xaml.Interactions.DragAndDrop;
using BPFTP.Handlers;
using BPFTP.Services;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
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

        public bool IsSshSupported { get => RuntimeInformation.IsOSPlatform(OSPlatform.Windows); }

        [RelayCommand(CanExecute = nameof(ConnectionSelected))]
        private void Ssh()
        {
            if (SelectedConnection == null) return;

            var processStartInfo = new ProcessStartInfo
            {
                FileName = "cmd",
                Arguments = SelectedConnection.AuthMethod == Models.AuthroizeMethod.Password?
                    $"/K ssh {SelectedConnection.Username}@{SelectedConnection.Host} -p {SelectedConnection.Port}":
                    $"/K ssh -i {SelectedConnection.PrivateKeyPath} {SelectedConnection.Username}@{SelectedConnection.Host} -p {SelectedConnection.Port}",
                UseShellExecute = false,
                CreateNoWindow = false,
            };

            try
            {
                var process = Process.Start(processStartInfo);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to start SSH process for {Host}", SelectedConnection.Host);
                ViewOperation.ShowPopupShort(new NormalPopupViewModel() { Message = $"启动 SSH 失败: {ex.Message}" });
            }
        }
    }
}
