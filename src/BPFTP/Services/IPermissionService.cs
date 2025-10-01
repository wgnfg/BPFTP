using System.Threading.Tasks;

namespace BPFTP.Services
{
    public interface IPermissionService
    {
        Task<bool> RequestStoragePermissionAsync();
    }

    public class DummyPermissionService : IPermissionService
    {
        public Task<bool> RequestStoragePermissionAsync()
        {
            return Task.FromResult(true);
        }
    }
}
