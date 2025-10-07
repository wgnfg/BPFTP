using System.Threading.Tasks;

namespace BPFTP.Services
{
    public interface ISecureCredentialService
    {
        Task StoreCredentialAsync(string key, string credential);

        Task<string?> RetrieveCredentialAsync(string key);

        Task DeleteCredentialAsync(string key);
    }

    public class DummySecureCredentialService : ISecureCredentialService
    {
        public Task DeleteCredentialAsync(string key)
        {
            return Task.CompletedTask;
        }

        public Task<string?> RetrieveCredentialAsync(string key)
        {
            return Task.FromResult<string?>(null);
        }

        public Task StoreCredentialAsync(string key, string credential)
        {
            return Task.CompletedTask;
        }
    }
}
