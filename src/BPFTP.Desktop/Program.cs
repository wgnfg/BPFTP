using Avalonia;
using BPFTP.Desktop.Services;
using BPFTP.Services;
using BPFTP.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using SqlSugar;
using System;
using System.Runtime.Versioning;
namespace BPFTP.Desktop;

class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        StaticConfig.EnableAot = true;
        ConfigureServices(App.ServicesCollection);
        BuildAvaloniaApp()
        .StartWithClassicDesktopLifetime(args);
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace()
            .AfterPlatformServicesSetup(b => App.ServicesCollection.AddLogging())
            .UseR3();
    public static void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<SftpWorkspaceViewModel, SftpWorkspaceViewModel>();
        services.AddSingleton<IPermissionService, DesktopPermissionService>();
        if (OperatingSystem.IsWindows())
        {
            services.AddSingleton<ISecureCredentialService, DesktopSecureCredentialService>();
        }
        if (OperatingSystem.IsLinux())
        {
            services.AddSingleton<ISecureCredentialService, DummySecureCredentialService>();
        }
        if (OperatingSystem.IsMacOS())
        {
            services.AddSingleton<ISecureCredentialService, DummySecureCredentialService>();
        }
    }

}