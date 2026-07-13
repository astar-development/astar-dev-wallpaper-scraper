using System;
using AStar.Dev.Wallpaper.Scraper.Services;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Events;
using System.Globalization;
using Testably.Abstractions;
using AStar.Dev.Wallpaper.Scraper.Startup;
using AStar.Dev.Wallpaper.Scraper.Configuration;
namespace AStar.Dev.Wallpaper.Scraper;

public partial class App : Application, IDisposable
{
    private bool disposedValue;

    private ServiceProvider? services;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        var fileSystem = new RealFileSystem();
        var collection = new ServiceCollection()
            .AddApplicationServices();

        var configuration = RegisterOptions(collection);
        ConfigureSerilog(fileSystem, configuration);

        services = collection
            .AddLogging(logging => logging.AddSerilog(dispose: true))
            .BuildServiceProvider();


        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = services.GetRequiredService<MainWindow>();

            // Fire-and-forget: the check runs in the background and prompts via a
            // dialog only when an update has already been downloaded.
            _ = services.GetRequiredService<UpdateService>().CheckForUpdatesAsync(desktop.MainWindow);
        }

        base.OnFrameworkInitializationCompleted();
    }
    private static IConfigurationRoot RegisterOptions(IServiceCollection services)
    {
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();
        _ = services.AddOptions<EntraIdConfiguration>()
                .Bind(configuration.GetSection(EntraIdConfiguration.SectionName))
                .ValidateDataAnnotations()
                .ValidateOnStart();
        _ = services.AddOptions<SyncSettings>()
                .Bind(configuration.GetSection(SyncSettings.SectionName))
                .ValidateDataAnnotations()
                .ValidateOnStart();
        _ = services.AddOptions<ClientConfiguration>()
                .Bind(configuration.GetSection(ClientConfiguration.SectionName))
                .ValidateDataAnnotations()
                .ValidateOnStart();

        return configuration;
    }

    private static void ConfigureSerilog(RealFileSystem fileSystem, IConfigurationRoot configuration)
    {
        _ = fileSystem.Directory.CreateDirectory(Startup.ApplicationDirectories.LogsDirectory);

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .ReadFrom.Configuration(configuration)
            .WriteTo.Console(formatProvider: CultureInfo.InvariantCulture)
            .WriteTo.File(
                formatter: new Serilog.Formatting.Json.JsonFormatter(),
                path: $"{Startup.ApplicationDirectories.LogsDirectory}/log.txt",
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 7,
                shared: true,
                flushToDiskInterval: TimeSpan.FromSeconds(1))
            .CreateLogger();
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                services?.Dispose();
            }

            disposedValue = true;
        }
    }

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}