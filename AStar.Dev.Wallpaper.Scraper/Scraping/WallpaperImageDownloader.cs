using Microsoft.Playwright;

namespace AStar.Dev.Wallpaper.Scraper.Scraping;

/// <summary>
///     Downloads a wallpaper's full-size image bytes.
/// </summary>
public sealed class WallpaperImageDownloader : IWallpaperImageDownloader
{
    private const int _navigationTimeoutMilliseconds = 30_000;

    /// <inheritdoc />
    public async Task<byte[]> DownloadAsync(IPage page, string imageUrl, CancellationToken token)
    {
        var response = await page.GotoAsync(imageUrl, new PageGotoOptions { Timeout = _navigationTimeoutMilliseconds, }).ConfigureAwait(false);

        return await response!.BodyAsync().ConfigureAwait(false);
    }
}
