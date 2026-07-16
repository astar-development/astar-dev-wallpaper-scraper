namespace AStar.Dev.Wallpaper.Scraper.Scraping;

/// <summary>
///     Resolves the directory a wallpaper should be saved under, from its curated tags.
/// </summary>
public static class WallpaperDirectoryResolver
{
    /// <summary>
    ///     Builds the directory path for a wallpaper, nesting one path segment per categorised tag: famous and
    ///     internet-model tags first, then the remaining tags, each in their original relative order.
    /// </summary>
    /// <param name="directories">The directory naming conventions to use.</param>
    /// <param name="tags">The wallpaper's curated tags.</param>
    public static string Resolve(DirectoryLayout directories, IReadOnlyList<TagData> tags)
    {
        var baseDirectory = directories.RootDirectory + (tags.Any(tag => tag.IsFamous) ? directories.BaseDirectoryFamous : directories.BaseDirectory);

        return tags
            .Where(tag => tag.IsFamous || tag.IsInternet)
            .Concat(tags.Where(tag => !tag.IsFamous && !tag.IsInternet))
            .Where(tag => tag.Category is not null)
            .Aggregate(baseDirectory, (directory, tag) => Path.Combine(directory, tag.Tag));
    }
}
