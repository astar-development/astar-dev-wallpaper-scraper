namespace AStar.Dev.Wallpaper.Scraper.Scraping;

/// <summary>
///     Publishes newly generated wallpaper thumbnails as they become available during a scrape.
/// </summary>
public interface IWallpaperThumbnailPublisher
{
    /// <summary>
    ///     Publishes a PNG-encoded thumbnail to any subscribers.
    /// </summary>
    /// <param name="thumbnailBytes">The PNG-encoded thumbnail bytes.</param>
    void Publish(byte[] thumbnailBytes);
}
