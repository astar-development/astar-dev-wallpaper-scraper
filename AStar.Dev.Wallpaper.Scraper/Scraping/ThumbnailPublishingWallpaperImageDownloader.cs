using Microsoft.Playwright;

namespace AStar.Dev.Wallpaper.Scraper.Scraping;

/// <summary>
///     Decorates <see cref="IWallpaperImageDownloader" /> so that a thumbnail is generated and published for every
///     downloaded wallpaper, without the downloader itself needing to know about thumbnails.
/// </summary>
public sealed class ThumbnailPublishingWallpaperImageDownloader(
    IWallpaperImageDownloader inner,
    IWallpaperThumbnailGenerator thumbnailGenerator,
    IWallpaperThumbnailPublisher thumbnailPublisher) : IWallpaperImageDownloader
{
    /// <inheritdoc />
    public async Task<byte[]> DownloadAsync(IPage page, string imageUrl, CancellationToken token)
    {
        var imageBytes = await inner.DownloadAsync(page, imageUrl, token);
        thumbnailPublisher.Publish(thumbnailGenerator.Generate(imageBytes));

        return imageBytes;
    }
}
