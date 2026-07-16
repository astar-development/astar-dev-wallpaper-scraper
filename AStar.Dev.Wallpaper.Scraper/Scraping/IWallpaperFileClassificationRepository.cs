namespace AStar.Dev.Wallpaper.Scraper.Scraping;

/// <summary>
///     Persists downloaded wallpapers and their tag classifications.
/// </summary>
public interface IWallpaperFileClassificationRepository
{
    /// <summary>
    ///     Determines whether a file already exists at the given directory, whose name contains the given text.
    /// </summary>
    /// <param name="directoryPath">The directory the file would be saved under.</param>
    /// <param name="fileNameContains">Text the file name must contain, e.g. an identifier known before the final file name.</param>
    /// <param name="token">A token used to observe cancellation of the read.</param>
    Task<bool> IsAlreadyDownloadedAsync(string directoryPath, string fileNameContains, CancellationToken token);

    /// <summary>
    ///     Records a file classification for every tag, linking the wallpaper to its classification category.
    /// </summary>
    /// <param name="tags">The wallpaper's curated tags.</param>
    /// <param name="imageUrl">The full-size wallpaper image URL, used to derive the file name.</param>
    /// <param name="directoryPath">The directory the wallpaper was saved under.</param>
    /// <param name="sizeBytes">The saved file's size, in bytes.</param>
    /// <param name="dimensions">The saved image's pixel dimensions.</param>
    /// <param name="token">A token used to observe cancellation of the write.</param>
    Task RecordAsync(IReadOnlyList<TagData> tags, string imageUrl, string directoryPath, long sizeBytes, ImageDimensions dimensions, CancellationToken token);
}
