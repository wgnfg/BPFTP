
using System;
using System.IO;
using System.Runtime.Versioning;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using BPFTP.Services;

namespace BPFTP.Desktop.Services;

/// <summary>
/// Desktop implementation of ISecureCredentialService using Windows DPAPI.
/// This service is only supported on Windows.
/// </summary>
[SupportedOSPlatform("windows")]
public class DesktopSecureCredentialService : ISecureCredentialService
{
    private readonly string _storagePath;

    public DesktopSecureCredentialService()
    {
        // Store credentials in a dedicated folder within the user's local app data directory.
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        _storagePath = Path.Combine(appDataPath, "BPFTP");
        Directory.CreateDirectory(_storagePath);
    }

    private string GetFilePath(string key) => Path.Combine(_storagePath, key);

    public Task StoreCredentialAsync(string key, string credential)
    {
        if (string.IsNullOrEmpty(credential))
        {
            // If the credential is null or empty, delete any existing entry.
            DeleteCredentialAsync(key);
            return Task.CompletedTask;
        }

        var credentialBytes = Encoding.UTF8.GetBytes(credential);

        // Encrypt the data using the current user's context.
        var encryptedBytes = ProtectedData.Protect(credentialBytes, null, DataProtectionScope.CurrentUser);

        var encryptedString = Convert.ToBase64String(encryptedBytes);
        
        var filePath = GetFilePath(key);
        return File.WriteAllTextAsync(filePath, encryptedString);
    }

    public async Task<string?> RetrieveCredentialAsync(string key)
    {
        var filePath = GetFilePath(key);
        if (!File.Exists(filePath))
        {
            return null;
        }

        try
        {
            var encryptedString = await File.ReadAllTextAsync(filePath);
            var encryptedBytes = Convert.FromBase64String(encryptedString);

            // Decrypt the data using the current user's context.
            var credentialBytes = ProtectedData.Unprotect(encryptedBytes, null, DataProtectionScope.CurrentUser);

            return Encoding.UTF8.GetString(credentialBytes);
        }
        catch (Exception ex) when (ex is CryptographicException or FileNotFoundException)
        {
            // Handle cases where the file is missing or decryption fails.
            // You might want to log this exception.
            return null;
        }
    }

    public Task DeleteCredentialAsync(string key)
    {
        var filePath = GetFilePath(key);
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }
        return Task.CompletedTask;
    }
}
