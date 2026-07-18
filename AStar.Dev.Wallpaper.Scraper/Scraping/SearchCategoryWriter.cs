using AStar.Dev.FunctionalParadigm;
using AStar.Dev.Infrastructure.AppDb;
using AStar.Dev.Infrastructure.AppDb.Entities;
using Microsoft.EntityFrameworkCore;

namespace AStar.Dev.Wallpaper.Scraper.Scraping;

/// <inheritdoc cref="ISearchCategoryWriter" />
public class SearchCategoryWriter(IDbContextFactory<AppDbContext> dbContextFactory) : ISearchCategoryWriter
{
    /// <inheritdoc cref="ISearchCategoryWriter.WriteAsync" />
    public async Task<Exceptional<Exception>> WriteAsync(SearchCategoryDto searchCategory, CancellationToken cancellationToken)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        var existingCategory = await context.SearchCategories.FirstOrDefaultAsync(c => c.Name == searchCategory.Name, cancellationToken);

        if (existingCategory == null)
        {
            var newCategory = new SearchCategoryEntity
            {
                Name = searchCategory.Name,
                IsFamous = searchCategory.IsFamous,
                IsInternet = searchCategory.IsInternet,
                TotalPages = searchCategory.TotalPages,
                LastKnownImageCount = searchCategory.LastKnownImageCount,
                LastPageVisited = searchCategory.LastPageVisited,
                CreatedAt = DateTime.UtcNow
            };

            context.SearchCategories.Add(newCategory);
        }
        else
        {
            existingCategory.IsFamous = searchCategory.IsFamous;
            existingCategory.IsInternet = searchCategory.IsInternet;
            existingCategory.TotalPages = searchCategory.TotalPages;
            existingCategory.LastKnownImageCount = searchCategory.LastKnownImageCount;
            existingCategory.LastPageVisited = searchCategory.LastPageVisited;
            existingCategory.UpdatedAt = DateTime.UtcNow;

            context.SearchCategories.Update(existingCategory);
        }

        await context.SaveChangesAsync(cancellationToken);

        return null!;
    }
}

public record SearchCategoryDto(string Name, bool IsFamous, bool IsInternet, int TotalPages, int LastKnownImageCount, int LastPageVisited);