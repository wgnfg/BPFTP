using BPFTP.Models;
using Renci.SshNet;
using System;
using System.Threading.Tasks;

namespace BPFTP.Services;


public class SftpService : IDisposable
{
    private SftpClient? _client;

    public bool IsConnected => _client?.IsConnected ?? false;

    public async Task ConnectAsync(ConnectionProfile profile)
    {
        if (_client?.IsConnected == true) _client.Disconnect();

        AuthenticationMethod authMethod;
        if (profile.AuthMethod ==  AuthroizeMethod.SSH && !string.IsNullOrEmpty(profile.PrivateKeyPath))
        {
            var keyPassword = profile.PrivateKeyPassword;
            var keyFile = new PrivateKeyFile(profile.PrivateKeyPath, keyPassword ?? string.Empty);
            authMethod = new PrivateKeyAuthenticationMethod(profile.Username, keyFile);
        }
        else
        {
            var password = profile.Password;
            authMethod = new PasswordAuthenticationMethod(profile.Username, password ?? string.Empty);
        }

        var connectionInfo = new ConnectionInfo(profile.Host, profile.Port, profile.Username, authMethod);
        _client = new SftpClient(connectionInfo);
        await _client.ConnectAsync(default);
    }

    public void Disconnect() => _client?.Disconnect();

    public void Dispose()
    {
        _client?.Dispose();
        GC.SuppressFinalize(this);
    }
}
