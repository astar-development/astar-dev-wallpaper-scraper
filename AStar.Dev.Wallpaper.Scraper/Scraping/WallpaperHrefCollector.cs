using AStar.Dev.Wallpaper.Scraper.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.Playwright;

namespace AStar.Dev.Wallpaper.Scraper.Scraping;

/// <summary>
///     Collects wallpaper detail-page links from a search results page.
/// </summary>
public sealed class WallpaperHrefCollector(IOptions<ScrapeConfiguration> scrapeConfiguration) : IWallpaperHrefCollector
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<string>> CollectAsync(IPage page, CancellationToken token)
    {
        var previews = await page.GetByRole(AriaRole.Link).AllAsync().ConfigureAwait(false);
        List<string> hrefs = [];
        var baseUrl = scrapeConfiguration.Value.SearchConfiguration.BaseUrl.ToString().TrimEnd('/');

        foreach (var preview in previews)
        {
            var href = await preview.GetAttributeAsync("href").ConfigureAwait(false);

            if (href?.StartsWith($"{baseUrl}/w/", StringComparison.InvariantCultureIgnoreCase) == true)
                hrefs.Add(href);
        }

        return hrefs;
    }
}
