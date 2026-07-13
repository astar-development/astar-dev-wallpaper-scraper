using AStar.Dev.FunctionalParadigm;
using AStar.Dev.Wallpaper.Scraper.Configuration;
using AStar.Dev.Wallpaper.Scraper.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace AStar.Dev.Wallpaper.Scraper.Tests.Unit.Services;

public sealed class GivenPlaywrightService : IDisposable
{
    private readonly string userDataDirectory = Path.Combine(Path.GetTempPath(), $"playwright-profile-{Guid.NewGuid():N}");

    [Fact]
    public async Task when_configure_playwright_async_is_called_then_it_returns_an_ipage_instance()
    {
        var sut = CreatePlaywrightService();

        var result = await sut.ConfigurePlaywrightAsync();

        result.ShouldBeAssignableTo<Exceptional<Microsoft.Playwright.IPage>>();
    }

    [Fact]
    public async Task when_configure_playwright_async_is_called_then_the_browser_profile_is_persisted_to_the_configured_user_data_directory()
    {
        var sut = CreatePlaywrightService();

        var result = await sut.ConfigurePlaywrightAsync();

        var page = result.ShouldBeOfType<Success<Microsoft.Playwright.IPage>>().Value;
        await page.Context.CloseAsync();
        Directory.Exists(userDataDirectory).ShouldBeTrue();
        Directory.EnumerateFileSystemEntries(userDataDirectory).ShouldNotBeEmpty();
    }

    [Fact]
    public async Task when_configure_playwright_async_is_called_then_the_browser_is_launched_using_the_chrome_channel()
    {
        var sut = CreatePlaywrightService();

        var result = await sut.ConfigurePlaywrightAsync();

        var page = result.ShouldBeOfType<Success<Microsoft.Playwright.IPage>>().Value;
        await page.RouteAsync("https://localhost/**", route => route.FulfillAsync(new Microsoft.Playwright.RouteFulfillOptions { ContentType = "text/html", Body = "<html></html>" }));
        await page.GotoAsync("https://localhost/browser-brand-check");
        var brands = await page.EvaluateAsync<string>("JSON.stringify((navigator.userAgentData?.brands ?? []).map(b => b.brand))");
        await page.Context.CloseAsync();
        brands.ShouldContain("Google Chrome");
    }

    [Fact]
    public async Task when_created_then_it_implements_iasyncdisposable()
    {
        var sut = CreatePlaywrightService();

        sut.ShouldBeAssignableTo<IAsyncDisposable>();
    }

    public void Dispose()
    {
        if (Directory.Exists(userDataDirectory))
        {
            Directory.Delete(userDataDirectory, true);
        }
    }

    private IPlaywrightService CreatePlaywrightService()
    {
        var logger = NullLoggerFactory.Instance.CreateLogger<PlaywrightService>();
        var scrapeConfiguration = Options.Create(new ScrapeConfiguration { UserDataDirectory = userDataDirectory });

        return new PlaywrightService(logger, scrapeConfiguration);
    }
}
