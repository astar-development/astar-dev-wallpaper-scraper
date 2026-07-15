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
    public async Task EnsureCategoriesExistAsync(IReadOnlyList<TagData> tags, CancellationToken token)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync(token);

        foreach (var tag in tags.Where(tag => !string.IsNullOrWhiteSpace(tag.Category) && !string.IsNullOrWhiteSpace(tag.Tag)))
        {
            if (await context.FileClassificationCategories.AnyAsync(category => category.Name == tag.Tag, token))
                continue;

            context.FileClassificationCategories.Add(new FileClassificationCategoryEntity
            {
                Name = tag.Tag,
                IsFamous = tag.IsFamous,
                IsInternet = tag.IsInternet,
                IncludeInSearch = true,
            });
        }

        await context.SaveChangesAsync(token);
    }
}
