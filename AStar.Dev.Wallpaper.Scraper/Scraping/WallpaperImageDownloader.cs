using Microsoft.Playwright;

namespace AStar.Dev.Wallpaper.Scraper.Scraping;

/// <summary>
///     Downloads a wallpaper's full-size image bytes.
/// </summary>
public sealed class WallpaperImageDownloader : IWallpaperImageDownloader
{
    private const int NavigationTimeoutMilliseconds = 30_000;

    /// <inheritdoc />
    public async Task<byte[]> DownloadAsync(IPage page, string imageUrl, CancellationToken token)
    {
        var response = await page.GotoAsync(imageUrl, new PageGotoOptions { Timeout = NavigationTimeoutMilliseconds, }).ConfigureAwait(false);

        return await response!.BodyAsync().ConfigureAwait(false);
    }
}
