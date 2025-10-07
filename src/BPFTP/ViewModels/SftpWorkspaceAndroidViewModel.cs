using BPFTP.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BPFTP.ViewModels
{
    public partial class SftpWorkspaceViewModelForAndroid(DatabaseService databaseService, SftpService sftpService, ViewService viewService, FileService fileService, IPermissionService permissionService, ISecureCredentialService secureCredentialService) : SftpWorkspaceViewModel(databaseService, sftpService, viewService, fileService, permissionService, secureCredentialService)
    {
    }
}
