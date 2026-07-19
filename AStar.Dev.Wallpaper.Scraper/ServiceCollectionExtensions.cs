using System.IO.Abstractions;
using AStar.Dev.Infrastructure.AppDb;
using AStar.Dev.Wallpaper.Scraper.Configuration;
using AStar.Dev.Wallpaper.Scraper.Configuration.EntityEditor;
using AStar.Dev.Wallpaper.Scraper.Home;
using AStar.Dev.Wallpaper.Scraper.Scraping;
using AStar.Dev.Wallpaper.Scraper.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Testably.Abstractions;

namespace AStar.Dev.Wallpaper.Scraper;

/// <summary>Registers the application's services with the dependency injection container.</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>Registers configuration bindings, scraping services, and UI services with the dependency injection container.</summary>
    /// <param name="services">The service collection to register the application's services with.</param>
    /// <param name="configuration">The application configuration used to bind the options sections.</param>
    /// <returns>The <paramref name="services" /> collection to allow further chaining.</returns>
    public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton(configuration);
        services.Configure<ScrapeConfiguration>(configuration.GetSection(nameof(ScrapeConfiguration)));
        services.Configure<UpdateConfiguration>(configuration.GetSection(nameof(UpdateConfiguration)));

        services.AddSingleton<IFileSystem, RealFileSystem>();
        services.AddSingleton<IPlaywrightService, PlaywrightService>();
        services.AddSingleton<ISearchCategoryWriter, SearchCategoryWriter>();
        services.AddSingleton<ISearchCategoryReader, SearchCategoryReader>();
        services.AddSingleton<UpdateService>();
        services.AddSingleton<MainWindowViewModel>();
        services.AddSingleton<MainWindow>();
        services.AddDbContextFactory<AppDbContext>((serviceProvider, options) =>
            options.UseSqlite(serviceProvider.GetRequiredService<IOptions<ScrapeConfiguration>>().Value.ConnectionStrings.Sqlite));
        services.AddSingleton<IScrapeContextReader, ScrapeContextReader>();
        services.AddSingleton<IWallpaperCountReader, WallpaperCountReader>();
        services.AddSingleton<IWallpaperHrefCollector, WallpaperHrefCollector>();
        services.AddSingleton<ITagReader, TagReader>();
        services.AddSingleton<IWallpaperImageLocator, WallpaperImageLocator>();
        services.AddSingleton<WallpaperImageDownloader>();
        services.AddSingleton<IWallpaperThumbnailGenerator, WallpaperThumbnailGenerator>();
        services.AddSingleton<WallpaperThumbnailBroadcaster>();
        services.AddSingleton<IWallpaperThumbnailPublisher>(serviceProvider => serviceProvider.GetRequiredService<WallpaperThumbnailBroadcaster>());
        services.AddSingleton<IWallpaperThumbnailFeed>(serviceProvider => serviceProvider.GetRequiredService<WallpaperThumbnailBroadcaster>());
        services.AddSingleton<IWallpaperImageDownloader>(serviceProvider => new ThumbnailPublishingWallpaperImageDownloader(
            serviceProvider.GetRequiredService<WallpaperImageDownloader>(),
            serviceProvider.GetRequiredService<IWallpaperThumbnailGenerator>(),
            serviceProvider.GetRequiredService<IWallpaperThumbnailPublisher>()));
        services.AddSingleton<IImageDimensionsReader, SkiaImageDimensionsReader>();
        services.AddSingleton<IWallpaperFileStore, WallpaperFileStore>();
        services.AddSingleton<IWallpaperCategoryRegistrar, WallpaperCategoryRegistrar>();
        services.AddSingleton<IWallpaperFileClassificationRepository, WallpaperFileClassificationRepository>();
        services.AddSingleton<IScrapeAction, SearchCategoryScrapeAction>();
        services.AddSingleton<IEntityEditorFactory>(serviceProvider => new EntityEditorFactory(
            serviceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>(),
            serviceProvider.GetRequiredService<IFileSystem>(),
            ApplicationDirectories.DocumentsExportDirectory));
        services.AddTransient<Clock>(serviceProvider => () => DateTimeOffset.Now);

        return services;
    }
}
