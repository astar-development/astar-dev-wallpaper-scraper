using AStar.Dev.FunctionalParadigm;
using AStar.Dev.Logging.Extensions;
using AStar.Dev.Wallpaper.Scraper.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Playwright;

namespace AStar.Dev.Wallpaper.Scraper.Services;

/// <inheritdoc />
public class PlaywrightService(ILogger<PlaywrightService> logger, IOptions<ScrapeConfiguration> scrapeConfiguration) : IPlaywrightService, IAsyncDisposable
{
    private readonly SemaphoreSlim configureLock = new(1, 1);
    private IPlaywright? playwright;
    private Microsoft.Playwright.IBrowserContext? context;
    private Microsoft.Playwright.IPage? page;

    /// <inheritdoc />
    public async Task<Exceptional<Microsoft.Playwright.IPage>> ConfigurePlaywrightAsync()
    {
        await configureLock.WaitAsync();

        try
        {
            if (page is not null)
            {
                return Exceptional.Success(page);
            }

            return await Try.RunAsync(async () =>
                {
                    playwright ??= await Microsoft.Playwright.Playwright.CreateAsync();
                    context = await playwright.Chromium.LaunchPersistentContextAsync(scrapeConfiguration.Value.UserDataDirectory, new Microsoft.Playwright.BrowserTypeLaunchPersistentContextOptions
                    {
                        BaseURL = EnsureTrailingSlash(scrapeConfiguration.Value.SearchConfiguration.BaseUrl),
                        Channel = "chrome",
                        Headless = scrapeConfiguration.Value.SearchConfiguration.UseHeadless,
                        ViewportSize = new ViewportSize { Width = 3000, Height = 1200 },
                        Locale = "en-GB",
                        TimezoneId = "Europe/London",
                    });
                    page = await context.NewPageAsync();

                    LogMessage.Information(logger, "Playwright configured successfully.");

                    return page;
                });
        }
        finally
        {
            configureLock.Release();
        }
    }

    private static string EnsureTrailingSlash(Uri baseUrl)
    {
        var url = baseUrl.ToString();

        return url.EndsWith('/') ? url : $"{url}/";
    }

    public async ValueTask DisposeAsync()
    {
        if (context is not null)
        {
            await context.CloseAsync();
        }

        playwright?.Dispose();
        configureLock.Dispose();
        GC.SuppressFinalize(this);
    }
}
