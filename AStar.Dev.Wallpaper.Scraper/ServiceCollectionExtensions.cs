using System.IO.Abstractions;
using AStar.Dev.Wallpaper.Scraper.Configuration;
using AStar.Dev.Wallpaper.Scraper.Home;
using AStar.Dev.Wallpaper.Scraper.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Testably.Abstractions;

namespace AStar.Dev.Wallpaper.Scraper;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton(configuration);
        services.Configure<ScrapeConfiguration>(configuration.GetSection(nameof(ScrapeConfiguration)));
        services.Configure<UpdateConfiguration>(configuration.GetSection(nameof(UpdateConfiguration)));

        services.AddSingleton<IFileSystem, RealFileSystem>();
        services.AddSingleton<IPlaywrightService, PlaywrightService>();
        services.AddSingleton<UpdateService>();
        services.AddSingleton<MainWindowViewModel>();
        services.AddSingleton<MainWindow>();

        return services;
    }
}
