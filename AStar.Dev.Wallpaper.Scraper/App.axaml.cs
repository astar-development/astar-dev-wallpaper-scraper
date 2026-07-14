using AStar.Dev.Infrastructure.AppDb;
using AStar.Dev.Logging.Extensions;
using AStar.Dev.Wallpaper.Scraper.Services;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using System.Globalization;
using Testably.Abstractions;
using AStar.Dev.Wallpaper.Scraper.Configuration;
using AStar.Dev.Wallpaper.Scraper.Home;
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
        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddUserSecrets<App>(optional: true)
            .AddEnvironmentVariables()
            .Build();

        var collection = new ServiceCollection()
            .AddApplicationServices(configuration);

        RegisterOptions(collection, configuration);
        ConfigureSerilog(fileSystem, configuration);

        services = collection
            .AddLogging(logging => logging.AddSerilog(dispose: true))
            .BuildServiceProvider();

        MigrateDatabaseAsync(services).GetAwaiter().GetResult();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = services.GetRequiredService<MainWindow>();

            _ = services.GetRequiredService<UpdateService>().CheckForUpdatesAsync(desktop.MainWindow);
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static async Task MigrateDatabaseAsync(IServiceProvider serviceProvider)
    {
        var logger = serviceProvider.GetRequiredService<ILogger<App>>();

        try
        {
            var dbContextFactory = serviceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>();
            await using var dbContext = await dbContextFactory.CreateDbContextAsync();
            await dbContext.Database.MigrateAsync();
        }
        catch (Exception exception)
        {
            LogMessage.Error(logger, "Database migration failed", exception);
        }
    }
    private static void RegisterOptions(IServiceCollection services, IConfiguration configuration) =>
        _ = services.AddOptions<SyncSettings>()
                .Bind(configuration.GetSection(SyncSettings.SectionName))
                .ValidateDataAnnotations()
                .ValidateOnStart();

    private static void ConfigureSerilog(RealFileSystem fileSystem, IConfigurationRoot configuration)
    {
        _ = fileSystem.Directory.CreateDirectory(ApplicationDirectories.LogsDirectory);

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .ReadFrom.Configuration(configuration)
            .WriteTo.Console(formatProvider: CultureInfo.InvariantCulture)
            .WriteTo.File(
                formatter: new Serilog.Formatting.Json.JsonFormatter(),
                path: $"{ApplicationDirectories.LogsDirectory}/log.txt",
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
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method - Do NOT remove this comment.
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}