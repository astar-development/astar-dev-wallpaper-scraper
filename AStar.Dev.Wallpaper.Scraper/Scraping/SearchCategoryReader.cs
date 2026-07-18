using AStar.Dev.FunctionalParadigm;
using AStar.Dev.Infrastructure.AppDb;
using AStar.Dev.Utilities;
using Microsoft.EntityFrameworkCore;

namespace AStar.Dev.Wallpaper.Scraper.Scraping;

/// <inheritdoc cref="ISearchCategoryReader" />
public sealed class SearchCategoryReader(IDbContextFactory<AppDbContext> dbContextFactory) : ISearchCategoryReader
{
    /// <inheritdoc cref="ISearchCategoryReader.GetLastKnownImageCountAsync" />
    public async Task<Option<int>> GetLastKnownImageCountAsync(string categoryName, CancellationToken cancellationToken)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        var category = await context.SearchCategories.FirstOrDefaultAsync(c => c.Name == categoryName, cancellationToken);

        return category is null ? Option.None<int>() : new Option<int>.Some(category.LastKnownImageCount);
    }
}
