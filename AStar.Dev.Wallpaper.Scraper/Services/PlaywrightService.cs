using System.IO.Abstractions;
using AStar.Dev.FunctionalParadigm;
using AStar.Dev.Logging.Extensions;
using AStar.Dev.Wallpaper.Scraper.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Playwright;

namespace AStar.Dev.Wallpaper.Scraper.Services;

/// <inheritdoc />
public class PlaywrightService(ILogger<PlaywrightService> logger, IOptions<ScrapeConfiguration> scrapeConfiguration, IFileSystem fileSystem) : IPlaywrightService, IAsyncDisposable
{
    private readonly SemaphoreSlim configureLock = new(1, 1);
    private IPlaywright? playwright;
    private IBrowserContext? context;
    private IPage? page;

    /// <inheritdoc />
    public async Task<Exceptional<IPage>> ConfigurePlaywrightAsync(CancellationToken token)
    {
        var lockAcquired = false;

        return await Try.RunAsync(async () =>
        {
            if (page is not null) return page;

            await configureLock.WaitAsync(token);
            lockAcquired = true;
            fileSystem.Directory.CreateDirectory(scrapeConfiguration.Value.UserDataDirectory);

            return null;
        })
        .TapAsync(_ =>
        {
            if (lockAcquired) LogMessage.Information(logger, "User data directory created successfully.");
        })
        .MapAsync(async _ => (playwright ??= await Playwright.CreateAsync().ConfigureAwait(false)))
        .MapAsync(async _ => (context ??= await playwright!.Chromium.LaunchPersistentContextAsync(scrapeConfiguration.Value.UserDataDirectory, SetContext()).ConfigureAwait(false)))
        .MapAsync(async context =>
        {
            await context!.AddInitScriptAsync(HideWebdriverScript).ConfigureAwait(false);

            return context;
        })
        .TapAsync(_ =>
        {
            if (lockAcquired) LogMessage.Information(logger, "Playwright configured successfully.");
        })
        .MapAsync(async context =>
        {
            return page ??= await context!.NewPageAsync()!;
        })
        .Ensure(_ =>
        {
            if (lockAcquired) configureLock.Release();
        });
    }

    private const string HideWebdriverScript = "Object.defineProperty(navigator, 'webdriver', { get: () => undefined });";

    private BrowserTypeLaunchPersistentContextOptions SetContext()
    {
        return new BrowserTypeLaunchPersistentContextOptions
        {
            BaseURL = EnsureTrailingSlash(scrapeConfiguration.Value.SearchConfiguration.BaseUrl),
            Channel = "chrome",
            Headless = scrapeConfiguration.Value.SearchConfiguration.UseHeadless,
            Args = ["--disable-blink-features=AutomationControlled", "--password-store=kwallet6"],
            ViewportSize = new ViewportSize { Width = 3000, Height = 1200 },
            Locale = "en-GB",
            TimezoneId = "Europe/London",
        };
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
