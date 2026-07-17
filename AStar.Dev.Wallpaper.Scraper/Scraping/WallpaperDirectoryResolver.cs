using AStar.Dev.Utilities;

namespace AStar.Dev.Wallpaper.Scraper.Scraping;

/// <summary>
///     Resolves the directory a wallpaper should be saved under, from its curated tags.
/// </summary>
public static class WallpaperDirectoryResolver
{
    private const int MaxTagSegments = 6;
    private const int DrivePrefixLength = 5;

    /// <summary>
    ///     Builds the directory path for a wallpaper, nesting one path segment per categorised tag: famous and
    ///     internet-model tags first, then the remaining tags in alphabetical order. Tags without a category, or
    ///     matching the search category's name, contribute no path segment. At most six tag segments are used.
    /// </summary>
    /// <param name="directories">The directory naming conventions to use.</param>
    /// <param name="tags">The wallpaper's curated tags.</param>
    /// <param name="category">The search category the wallpaper was found under.</param>
    public static string Resolve(DirectoryLayout directories, IReadOnlyList<TagData> tags, ScrapeCategory category)
    {
        var baseDirectory = directories.RootDirectory + (tags.Any(tag => tag.IsFamous) ? directories.BaseDirectoryFamous : directories.BaseDirectory);
        var baseDirectoryWithCategory = baseDirectory.CombinePath(category.Name[0].ToString()).CombinePath(category.Name);

        var eligibleTags = tags.Where(tag => tag.Category is not null && !tag.Tag.Equals(category.Name, StringComparison.OrdinalIgnoreCase)).ToList();

        var orderedTags = eligibleTags
            .Where(tag => tag.IsFamous || tag.IsInternet)
            .Concat(eligibleTags.Where(tag => !tag.IsFamous && !tag.IsInternet).OrderBy(tag => tag.Tag))
            .Take(MaxTagSegments);

        var path = orderedTags.Aggregate(baseDirectoryWithCategory, (directory, tag) => directory.CombinePath(tag.Tag));
        var drivePrefixLength = Math.Min(DrivePrefixLength, path.Length);

        return path[..drivePrefixLength] + path[drivePrefixLength..].Replace(":", string.Empty).CleanPath();
    }
}
