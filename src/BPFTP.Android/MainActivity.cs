using Android.App;
using Android.Content.PM;

using Avalonia;
using Avalonia.Android;
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
        return base.CustomizeAppBuilder(builder)
            .WithInterFont()
            .UseR3();
    }

    public static void ConfigureServices(IServiceCollection services)
    {
    }
}
