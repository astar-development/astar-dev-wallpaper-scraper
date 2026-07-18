using AStar.Dev.Infrastructure.AppDb.Entities;
using AStar.Dev.Utilities;

namespace AStar.Dev.Wallpaper.Scraper.Scraping;

/// <summary>
///     Resolves the directory a wallpaper should be saved under, from its curated tags.
/// </summary>
public static class WallpaperDirectoryResolver
{
    private const int MaxTagSegments = 12;
    private const int DrivePrefixLength = 3;

    /// <summary>
    ///     Builds the directory path for a wallpaper, nesting one path segment per categorised tag: famous and
    ///     internet-model tags first, then the remaining tags in alphabetical order. Tags without a category, or
    ///     matching the search category's name, contribute no path segment. At most twelve tag segments are used.
    /// </summary>
    /// <param name="directoryLayout">The directory naming conventions to use.</param>
    /// <param name="tags">The wallpaper's curated tags.</param>
    /// <param name="category">The search category the wallpaper was found under.</param>
    public static string Resolve(DirectoryLayout directoryLayout, IReadOnlyList<TagData> tags, ScrapeCategory category, IReadOnlyList<FileClassificationCategoryEntity> fileClassifications)
    {
        var baseDirectory = directoryLayout.RootDirectory + (tags.Any(tag => tag.IsFamous) ? directoryLayout.BaseDirectoryFamous : directoryLayout.BaseDirectory);
        var baseDirectoryWithCategory = baseDirectory.CombinePath(category.Name[0].ToString()).CombinePath(category.Name);

        var eligibleTags = tags.Where(tag => tag.Category is not null && !tag.Tag.Equals(category.Name, StringComparison.OrdinalIgnoreCase)).ToList();
        var orderedFileClassifications = GetOrderedFileClassifications(fileClassifications, eligibleTags)
            .Take(MaxTagSegments);

        var path = orderedFileClassifications.Aggregate(baseDirectoryWithCategory, (directory, tag) => directory.CombinePath(tag.Name));
        var drivePrefixLength = Math.Min(DrivePrefixLength, path.Length);

        return path[..drivePrefixLength] + path[drivePrefixLength..].Replace(":", string.Empty).CleanPath();
    }

    private static IReadOnlyList<FileClassificationCategoryEntity> GetOrderedFileClassifications(IReadOnlyList<FileClassificationCategoryEntity> fileClassifications, List<TagData> eligibleTags)
        => [.. fileClassifications
                .Where(classification => classification.IncludeInSearch && eligibleTags.Any(tag => tag.Tag.CaseInsensitiveEquals(classification.Name)))
                .OrderByDescending(t => t.IsFamous)
                .ThenByDescending(t => t.IsInternet)
                .ThenBy(t => t.Level)];
}
