using AStar.Dev.Wallpaper.Scraper.Scraping;
using Microsoft.Playwright;

namespace AStar.Dev.Wallpaper.Scraper.Tests.Unit.Scraping;

public sealed class GivenCategoryPageExtractor
{
    [Fact]
    public async Task when_extract_async_is_called_then_it_returns_an_empty_list_pending_real_site_selectors()
    {
        var sut = new CategoryPageExtractor();
        var page = Substitute.For<IPage>();

        var result = await sut.ExtractAsync(page, TestContext.Current.CancellationToken);

        result.ShouldBeEmpty();
    }
}
