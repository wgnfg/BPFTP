using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using Avalonia.Platform;
using System.IO;
using BPFTP.Services;
using BPFTP.ViewModels;
using BPFTP.Views;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using System;

namespace BPFTP;

public partial class App : Application
{
    public static IServiceProvider? ServiceProvider { get; set; }
    public static IServiceCollection ServicesCollection { get; set; } = new ServiceCollection();
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        // Line below is needed to remove Avalonia data validation.
        // Without this line you will get duplicate validations from both Avalonia and CT
        //BindingPlugins.DataValidators.RemoveAt(0);
        ConfigureServices(ServicesCollection);
        ServiceProvider = ServicesCollection.BuildServiceProvider();

        var shellViewModel = ServiceProvider.GetRequiredService<ShellViewModel>();
        ViewOperation.Init();
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow
            {
                DataContext = shellViewModel
            };
        }
        else if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewPlatform)
        {
            singleViewPlatform.MainView = new MainView
            {
                DataContext = shellViewModel
            };
        }

        base.OnFrameworkInitializationCompleted();
    }

    public static void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<ShellViewModel>();

        services.AddSingleton<DatabaseService>();
        services.AddSingleton<SftpService>();
        services.AddSingleton<IViewService, ViewService>();
        services.AddSingleton<FileService>();
    }

}


public static class AppLogging
{
    public static IServiceCollection AddLogging(this IServiceCollection services)
    {
        using var stream = AssetLoader.Open(new Uri("avares://BPFTP/serilog.json"));
        var configuration = new ConfigurationBuilder()
            .AddJsonStream(stream)
            .Build();

        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(configuration)
            .CreateLogger();

        AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
        {
            Log.Fatal(e.ExceptionObject as Exception, "Unhandled exception");
        };
        
        services.AddLogging(builder =>
        {
            builder.AddSerilog(dispose: true);
        });

        return services;
    }

}
