using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace AStar.Dev.Wallpaper.Scraper.Scraping;

/// <summary>
///     Broadcasts wallpaper thumbnails from wherever they are generated during a scrape to any interested subscribers,
///     such as the main window's preview panel.
/// </summary>
public sealed class WallpaperThumbnailBroadcaster : IWallpaperThumbnailPublisher, IWallpaperThumbnailFeed, IDisposable
{
    private readonly Subject<byte[]> thumbnails = new();

    /// <inheritdoc />
    public IObservable<byte[]> Thumbnails => thumbnails.AsObservable();

    /// <inheritdoc />
    public void Publish(byte[] thumbnailBytes) => thumbnails.OnNext(thumbnailBytes);

    /// <summary>
    ///     Completes and disposes the underlying subject.
    /// </summary>
    public void Dispose() => thumbnails.Dispose();
}
