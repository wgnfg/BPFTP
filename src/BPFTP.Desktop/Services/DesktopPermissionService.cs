using BPFTP.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BPFTP.Desktop.Services
{
    public class DesktopPermissionService : IPermissionService
    {
        public Task<bool> RequestStoragePermissionAsync()
        {
            return Task.FromResult(true);
        }
    }
}
