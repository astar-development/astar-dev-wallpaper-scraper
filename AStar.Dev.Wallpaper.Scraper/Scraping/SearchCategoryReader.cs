using AStar.Dev.FunctionalParadigm;
using AStar.Dev.Infrastructure.AppDb;
using Microsoft.EntityFrameworkCore;

namespace AStar.Dev.Wallpaper.Scraper.Scraping;

/// <inheritdoc cref="ISearchCategoryReader" />
public sealed class SearchCategoryReader(IDbContextFactory<AppDbContext> dbContextFactory) : ISearchCategoryReader
{
    /// <inheritdoc cref="ISearchCategoryReader.GetLastKnownImageCountAsync" />
    public async Task<Option<int>> GetLastKnownImageCountAsync(string categoryName, CancellationToken cancellationToken)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        var categoryOption = await context.SearchCategories
            .Where(c => c.Name == categoryName)
            .AsAsyncEnumerable()
            .FirstOrNoneAsync(cancellationToken);

        return categoryOption.Map(category => category.LastKnownImageCount);
    }
}
