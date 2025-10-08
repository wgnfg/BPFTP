using BPFTP.Services;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BPFTP.ViewModels
{
    public partial class SftpWorkspaceForAndroidViewModel(DatabaseService databaseService, SftpService sftpService, IViewService viewService, FileService fileService, IPermissionService permissionService, ISecureCredentialService secureCredentialService, ILogger<SftpWorkspaceViewModel> logger) : SftpWorkspaceViewModel(databaseService, sftpService, viewService, fileService, permissionService, secureCredentialService, logger)
    {
    }
}
