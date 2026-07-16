using Microsoft.Playwright;

namespace AStar.Dev.Wallpaper.Scraper.Scraping;

/// <summary>
///     Downloads a wallpaper's full-size image bytes.
/// </summary>
public interface IWallpaperImageDownloader
{
    /// <summary>
    ///     Navigates to the image URL and reads its response body.
    /// </summary>
    /// <param name="page">The Playwright page to navigate.</param>
    /// <param name="imageUrl">The full-size wallpaper image URL.</param>
    /// <param name="token">A token used to observe cancellation of the download.</param>
    Task<byte[]> DownloadAsync(IPage page, string imageUrl, CancellationToken cancellationToken);
}
