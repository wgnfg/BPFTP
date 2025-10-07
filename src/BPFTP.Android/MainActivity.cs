using Android.App;
using Android.Content.PM;

using Avalonia;
using Avalonia.Android;
using BPFTP.Android.Services;
using BPFTP.Services;
using Microsoft.Extensions.DependencyInjection;

namespace BPFTP.Android;

[Activity(
    Label = "BPFTP.Android",
    Theme = "@style/MyTheme.NoActionBar",
    Icon = "@drawable/icon",
    MainLauncher = true,
    ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.UiMode)]
public class MainActivity : AvaloniaMainActivity<App>
{
    protected override AppBuilder CustomizeAppBuilder(AppBuilder builder)
    {
        ConfigureServices(App.ServicesCollection);
        AndroidPermissionService.MainActivity = this;
        return base.CustomizeAppBuilder(builder)
            .WithInterFont()
            .UseR3();
    }

    public static void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<IPermissionService, AndroidPermissionService>();
        services.AddSingleton<ISecureCredentialService, AndroidSecureCredentialService>();

    }

    public override void OnRequestPermissionsResult(int requestCode, string[] permissions, Permission[] grantResults)
    {
        AndroidPermissionService.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
    }
}
