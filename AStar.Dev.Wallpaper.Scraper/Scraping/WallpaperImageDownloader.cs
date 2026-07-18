using AStar.Dev.FunctionalParadigm;
using Microsoft.Playwright;

namespace AStar.Dev.Wallpaper.Scraper.Scraping;

/// <summary>
///     Downloads a wallpaper's full-size image bytes.
/// </summary>
public sealed class WallpaperImageDownloader : IWallpaperImageDownloader
{
    private const int NavigationTimeoutMilliseconds = 30_000;

    /// <inheritdoc />
    public async Task<Exceptional<byte[]>> DownloadAsync(IPage page, string imageUrl, string categoryName, IReadOnlyList<string> tags, CancellationToken cancellationToken) =>
        await Try.RunAsync(async () =>
        {
            var response = await page.GotoAsync(imageUrl, new PageGotoOptions { Timeout = NavigationTimeoutMilliseconds, }).ConfigureAwait(false);

            if (response is null)
            {
                throw new InvalidOperationException($"Navigating to '{imageUrl}' did not produce a response.");
            }

            return await response.BodyAsync().ConfigureAwait(false);
        }, cancellationToken);
}
