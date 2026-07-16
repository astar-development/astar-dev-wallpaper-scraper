using AStar.Dev.FunctionalParadigm;
using Microsoft.Playwright;

namespace AStar.Dev.Wallpaper.Scraper.Scraping;

/// <summary>
///     Decorates <see cref="IWallpaperImageDownloader" /> so that a thumbnail is generated and published for every
///     downloaded wallpaper, without the downloader itself needing to know about thumbnails.
/// </summary>
public sealed class ThumbnailPublishingWallpaperImageDownloader(IWallpaperImageDownloader inner, IWallpaperThumbnailGenerator thumbnailGenerator, IWallpaperThumbnailPublisher thumbnailPublisher) : IWallpaperImageDownloader
{
    /// <inheritdoc />
    public Task<Exceptional<byte[]>> DownloadAsync(IPage page, string imageUrl, CancellationToken cancellationToken) =>
        inner.DownloadAsync(page, imageUrl, cancellationToken)
            .TapAsync(imageBytes => thumbnailPublisher.Publish(thumbnailGenerator.Generate(imageBytes)));
}
