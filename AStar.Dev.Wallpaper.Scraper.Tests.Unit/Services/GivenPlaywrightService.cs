using AStar.Dev.FunctionalParadigm;
using AStar.Dev.Wallpaper.Scraper.Configuration;
using AStar.Dev.Wallpaper.Scraper.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Testably.Abstractions.Testing;

namespace AStar.Dev.Wallpaper.Scraper.Tests.Unit.Services;

public sealed class GivenPlaywrightService : IDisposable
{
    private readonly string userDataDirectory = Path.Combine(Path.GetTempPath(), $"playwright-profile-{Guid.NewGuid():N}");
    private readonly MockFileSystem fileSystem = new();

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
    public async Task when_configure_playwright_async_is_called_then_the_user_data_directory_is_created_via_the_injected_file_system()
    {
        var sut = CreatePlaywrightService();

        var result = await sut.ConfigurePlaywrightAsync();

        var page = result.ShouldBeOfType<Success<Microsoft.Playwright.IPage>>().Value;
        await page.Context.CloseAsync();
        fileSystem.Directory.Exists(userDataDirectory).ShouldBeTrue();
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
    public async Task when_configure_playwright_async_is_called_then_the_context_uses_the_configured_base_url()
    {
        var sut = CreatePlaywrightService();

        var result = await sut.ConfigurePlaywrightAsync();

        var page = result.ShouldBeOfType<Success<Microsoft.Playwright.IPage>>().Value;
        await page.RouteAsync("https://localhost/**", route => route.FulfillAsync(new Microsoft.Playwright.RouteFulfillOptions { ContentType = "text/html", Body = "<html></html>" }));
        await page.GotoAsync("/base-url-check");
        var url = page.Url;
        await page.Context.CloseAsync();
        url.ShouldBe("https://localhost/base-url-check");
    }

    [Fact]
    public async Task when_configure_playwright_async_is_called_then_the_browser_honours_the_configured_headless_setting()
    {
        var sut = CreatePlaywrightService(useHeadless: false);

        var result = await sut.ConfigurePlaywrightAsync();

        var page = result.ShouldBeOfType<Success<Microsoft.Playwright.IPage>>().Value;
        var userAgent = await page.EvaluateAsync<string>("navigator.userAgent");
        await page.Context.CloseAsync();
        userAgent.ShouldNotContain("HeadlessChrome");
    }

    [Fact]
    public async Task when_the_browser_launch_fails_then_a_failure_result_is_returned_instead_of_throwing()
    {
        var sut = CreatePlaywrightService(userDataDirectoryOverride: "\0invalid-user-data-directory");

        var result = await sut.ConfigurePlaywrightAsync();

        result.ShouldBeOfType<Failure<Microsoft.Playwright.IPage>>();
    }

    [Fact]
    public async Task when_the_base_url_has_a_path_segment_then_relative_navigation_resolves_beneath_that_path()
    {
        var sut = CreatePlaywrightService(baseUrlOverride: new Uri("https://localhost/gallery"));

        var result = await sut.ConfigurePlaywrightAsync();

        var page = result.ShouldBeOfType<Success<Microsoft.Playwright.IPage>>().Value;
        await page.RouteAsync("https://localhost/**", route => route.FulfillAsync(new Microsoft.Playwright.RouteFulfillOptions { ContentType = "text/html", Body = "<html></html>" }));
        await page.GotoAsync("page2");
        var url = page.Url;
        await page.Context.CloseAsync();
        url.ShouldBe("https://localhost/gallery/page2");
    }

    [Fact]
    public async Task when_configure_playwright_async_is_called_twice_then_the_same_page_instance_is_returned()
    {
        var sut = CreatePlaywrightService();

        var firstResult = await sut.ConfigurePlaywrightAsync();
        var secondResult = await sut.ConfigurePlaywrightAsync();

        var firstPage = firstResult.ShouldBeOfType<Success<Microsoft.Playwright.IPage>>().Value;
        var secondPage = secondResult.ShouldBeOfType<Success<Microsoft.Playwright.IPage>>().Value;
        await firstPage.Context.CloseAsync();
        secondPage.ShouldBeSameAs(firstPage);
    }

    [Fact]
    public async Task when_configure_playwright_async_is_called_concurrently_then_a_single_page_is_shared()
    {
        var sut = CreatePlaywrightService();

        var results = await Task.WhenAll(sut.ConfigurePlaywrightAsync(), sut.ConfigurePlaywrightAsync());

        var firstPage = results[0].ShouldBeOfType<Success<Microsoft.Playwright.IPage>>().Value;
        var secondPage = results[1].ShouldBeOfType<Success<Microsoft.Playwright.IPage>>().Value;
        await firstPage.Context.CloseAsync();
        secondPage.ShouldBeSameAs(firstPage);
    }

    [Fact]
    public async Task when_a_failed_launch_is_followed_by_a_corrected_configuration_then_a_page_is_returned()
    {
        var configuration = new ScrapeConfiguration { UserDataDirectory = "\0invalid-user-data-directory", SearchConfiguration = new SearchConfiguration { BaseUrl = new Uri("https://localhost"), UseHeadless = true } };
        var sut = new PlaywrightService(NullLoggerFactory.Instance.CreateLogger<PlaywrightService>(), Options.Create(configuration), fileSystem);

        var firstResult = await sut.ConfigurePlaywrightAsync();
        configuration.UserDataDirectory = userDataDirectory;
        var secondResult = await sut.ConfigurePlaywrightAsync();

        firstResult.ShouldBeOfType<Failure<Microsoft.Playwright.IPage>>();
        var page = secondResult.ShouldBeOfType<Success<Microsoft.Playwright.IPage>>().Value;
        await page.Context.CloseAsync();
    }

    [Fact]
    public async Task when_the_service_is_disposed_then_the_page_is_closed()
    {
        var sut = CreatePlaywrightService();
        var result = await sut.ConfigurePlaywrightAsync();
        var page = result.ShouldBeOfType<Success<Microsoft.Playwright.IPage>>().Value;

        await ((IAsyncDisposable)sut).DisposeAsync();

        page.IsClosed.ShouldBeTrue();
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

    private IPlaywrightService CreatePlaywrightService(bool useHeadless = true, string? userDataDirectoryOverride = null, Uri? baseUrlOverride = null)
    {
        var logger = NullLoggerFactory.Instance.CreateLogger<PlaywrightService>();
        var scrapeConfiguration = Options.Create(new ScrapeConfiguration { UserDataDirectory = userDataDirectoryOverride ?? userDataDirectory, SearchConfiguration = new SearchConfiguration { BaseUrl = baseUrlOverride ?? new Uri("https://localhost"), UseHeadless = useHeadless } });

        return new PlaywrightService(logger, scrapeConfiguration, fileSystem);
    }
}
