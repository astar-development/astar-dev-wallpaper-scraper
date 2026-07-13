using AStar.Dev.FunctionalParadigm;
using AStar.Dev.Logging.Extensions;
using AStar.Dev.Wallpaper.Scraper.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AStar.Dev.Wallpaper.Scraper.Services;

/// <inheritdoc />
public class PlaywrightService(ILogger<PlaywrightService> logger, IOptions<ScrapeConfiguration> scrapeConfiguration) : IPlaywrightService, IAsyncDisposable
{
    /// <inheritdoc />
    public async Task<Exceptional<Microsoft.Playwright.IPage>> ConfigurePlaywrightAsync()
        => await Try.RunAsync(async () =>
            {
                var playwright = await Microsoft.Playwright.Playwright.CreateAsync();
                var context = await playwright.Chromium.LaunchPersistentContextAsync(scrapeConfiguration.Value.UserDataDirectory, new Microsoft.Playwright.BrowserTypeLaunchPersistentContextOptions
                {
                    Channel = "chrome",
                    Headless = true
                });
                var page = await context.NewPageAsync();

                LogMessage.Information(logger, "Playwright configured successfully.");

                return page;
            });

    public async ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);
    }
}
