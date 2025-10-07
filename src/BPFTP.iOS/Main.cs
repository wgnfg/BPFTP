using BPFTP.Services;
using Microsoft.Extensions.DependencyInjection;
using UIKit;

namespace BPFTP.iOS;

public class Application
{
    // This is the main entry point of the application.
    static void Main(string[] args)
    {
        ConfigureServices(App.ServicesCollection);
        // if you want to use a different Application Delegate class from "AppDelegate"
        // you can specify it here.
        UIApplication.Main(args, null, typeof(AppDelegate));
    }

    public static void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<IPermissionService, DummyPermissionService>();
        services.AddSingleton<ISecureCredentialService, DummySecureCredentialService>();
    }
}
