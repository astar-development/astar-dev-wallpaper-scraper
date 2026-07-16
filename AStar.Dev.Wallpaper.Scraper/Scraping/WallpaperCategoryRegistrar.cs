using AStar.Dev.Infrastructure.AppDb;
using AStar.Dev.Infrastructure.AppDb.Entities;
using Microsoft.EntityFrameworkCore;

namespace AStar.Dev.Wallpaper.Scraper.Scraping;

/// <summary>
///     Ensures every tagged category observed while scraping exists in the file classification taxonomy.
/// </summary>
public sealed class WallpaperCategoryRegistrar(IDbContextFactory<AppDbContext> dbContextFactory) : IWallpaperCategoryRegistrar
{
    /// <inheritdoc />
    public async Task EnsureCategoriesExistAsync(IReadOnlyList<TagData> tags, CancellationToken cancellationToken)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        var categorisedTags = tags.Where(HasCategoryAndTag).ToList();

        foreach (var tag in categorisedTags)
        {
            if (await CategoryExistsAsync(context, tag, cancellationToken))
                continue;

            context.FileClassificationCategories.Add(CreateCategory(tag));
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    private static bool HasCategoryAndTag(TagData tag) => !string.IsNullOrWhiteSpace(tag.Category) && !string.IsNullOrWhiteSpace(tag.Tag);

    private static Task<bool> CategoryExistsAsync(AppDbContext context, TagData tag, CancellationToken cancellationToken) =>
        context.FileClassificationCategories.AnyAsync(category => category.Name == tag.Tag, cancellationToken);

    private static FileClassificationCategoryEntity CreateCategory(TagData tag) => new()
    {
        Name = tag.Tag,
        IsFamous = tag.IsFamous,
        IsInternet = tag.IsInternet,
        IncludeInSearch = true,
    };
}
