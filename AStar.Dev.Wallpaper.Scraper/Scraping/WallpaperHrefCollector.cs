using Microsoft.Playwright;

namespace AStar.Dev.Wallpaper.Scraper.Scraping;

/// <summary>
///     Collects wallpaper detail-page links from a search results page.
/// </summary>
public sealed class WallpaperHrefCollector : IWallpaperHrefCollector
{
    private const string _wallpaperPagePrefix = "https://wallhaven.cc/w/";

    /// <inheritdoc />
    public async Task<IReadOnlyList<string>> CollectAsync(IPage page, CancellationToken token)
    {
        var previews = await page.GetByRole(AriaRole.Link).AllAsync().ConfigureAwait(false);
        List<string> hrefs = [];

        foreach (var preview in previews)
        {
            var href = await preview.GetAttributeAsync("href").ConfigureAwait(false);

            if (href?.StartsWith(_wallpaperPagePrefix, StringComparison.InvariantCultureIgnoreCase) == true)
                hrefs.Add(href);
        }

        return hrefs;
    }
}
