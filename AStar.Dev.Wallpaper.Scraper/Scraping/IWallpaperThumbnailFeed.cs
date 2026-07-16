namespace AStar.Dev.Wallpaper.Scraper.Scraping;

/// <summary>
///     Exposes the stream of wallpaper thumbnails published during a scrape.
/// </summary>
public interface IWallpaperThumbnailFeed
{
    /// <summary>
    ///     Gets the stream of PNG-encoded thumbnail bytes, one per downloaded wallpaper.
    /// </summary>
    IObservable<byte[]> Thumbnails { get; }
}
