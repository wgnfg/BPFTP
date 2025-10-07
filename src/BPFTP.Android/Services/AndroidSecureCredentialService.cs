using System;
using System.Text;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.Security.Keystore;
using BPFTP.Services;
using Java.Security;
using Javax.Crypto;
using Javax.Crypto.Spec;

namespace BPFTP.Android.Services;

/// <summary>
/// Android implementation of ISecureCredentialService using the Android KeyStore and SharedPreferences.
/// This service creates a master key in the KeyStore to encrypt/decrypt credentials,
/// which are then stored in a private SharedPreferences file.
/// </summary>
public class AndroidSecureCredentialService : ISecureCredentialService
{
    private const string AndroidKeyStoreProvider = "AndroidKeyStore";
    private const string MasterKeyAlias = "bpfpt_master_credential_key";
    private const string CipherTransformation = "AES/GCM/NoPadding";
    private const string SharedPreferencesName = "bpfpt_secure_credentials";
    private const int GcmIvLength = 12; // GCM standard IV length is 12 bytes
    private const int GcmTagLength = 128; // GCM standard tag length is 128 bits

    private readonly ISharedPreferences _sharedPreferences;

    public AndroidSecureCredentialService()
    {
        try
        {
            var context = Application.Context;
            _sharedPreferences = context.GetSharedPreferences(SharedPreferencesName, FileCreationMode.Private);

            var keyStore = KeyStore.GetInstance(AndroidKeyStoreProvider);
            keyStore.Load(null);
            if (!keyStore.ContainsAlias(MasterKeyAlias))
            {
                CreateNewMasterKey();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to initialize AndroidSecureCredentialService: {ex.Message}");
            throw;
        }
    }

    private void CreateNewMasterKey()
    {
        var keyGenerator = KeyGenerator.GetInstance(KeyProperties.KeyAlgorithmAes, AndroidKeyStoreProvider);
        var spec = new KeyGenParameterSpec.Builder(
                MasterKeyAlias,
                KeyStorePurpose.Encrypt | KeyStorePurpose.Decrypt)
            .SetBlockModes(KeyProperties.BlockModeGcm)
            .SetEncryptionPaddings(KeyProperties.EncryptionPaddingNone)
            .SetRandomizedEncryptionRequired(true)
            .Build();

        keyGenerator.Init(spec);
        keyGenerator.GenerateKey();
    }

    public Task StoreCredentialAsync(string key, string credential)
    {
        return Task.Run(() =>
        {
            if (string.IsNullOrEmpty(credential))
            {
                DeleteCredentialAsync(key);
                return;
            }

            try
            {
                var keyStore = KeyStore.GetInstance(AndroidKeyStoreProvider);
                keyStore.Load(null);
                var secretKey = keyStore.GetKey(MasterKeyAlias, null);

                var cipher = Cipher.GetInstance(CipherTransformation);
                cipher.Init(CipherMode.EncryptMode, secretKey);

                var iv = cipher.GetIV();
                var credentialBytes = Encoding.UTF8.GetBytes(credential);
                var encryptedBytes = cipher.DoFinal(credentialBytes);

                var combined = new byte[iv.Length + encryptedBytes.Length];
                Buffer.BlockCopy(iv, 0, combined, 0, iv.Length);
                Buffer.BlockCopy(encryptedBytes, 0, combined, iv.Length, encryptedBytes.Length);

                var encryptedString = Convert.ToBase64String(combined);

                var editor = _sharedPreferences.Edit();
                editor.PutString(key, encryptedString);
                editor.Apply(); // Apply is asynchronous
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to store credential for key '{key}': {ex.Message}");
            }
        });
    }

    public Task<string?> RetrieveCredentialAsync(string key)
    {
        return Task.Run(() =>
        {
            try
            {
                var encryptedString = _sharedPreferences.GetString(key, null);
                if (string.IsNullOrEmpty(encryptedString))
                    return null;

                var combined = Convert.FromBase64String(encryptedString);

                var keyStore = KeyStore.GetInstance(AndroidKeyStoreProvider);
                keyStore.Load(null);
                var secretKey = keyStore.GetKey(MasterKeyAlias, null);

                var iv = new byte[GcmIvLength];
                var encryptedBytes = new byte[combined.Length - GcmIvLength];
                Buffer.BlockCopy(combined, 0, iv, 0, GcmIvLength);
                Buffer.BlockCopy(combined, GcmIvLength, encryptedBytes, 0, encryptedBytes.Length);

                var spec = new GCMParameterSpec(GcmTagLength, iv);
                var cipher = Cipher.GetInstance(CipherTransformation);
                cipher.Init(CipherMode.DecryptMode, secretKey, spec);

                var decryptedBytes = cipher.DoFinal(encryptedBytes);
                return Encoding.UTF8.GetString(decryptedBytes);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to retrieve credential for key '{key}': {ex.Message}");
                return null;
            }
        });
    }

    public Task DeleteCredentialAsync(string key)
    {
        var editor = _sharedPreferences.Edit();
        editor.Remove(key);
        editor.Apply();
        return Task.CompletedTask;
    }
}