using AStar.Dev.FunctionalParadigm;
using AStar.Dev.Infrastructure.AppDb;
using AStar.Dev.Infrastructure.AppDb.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Playwright;

namespace AStar.Dev.Wallpaper.Scraper.Scraping;

/// <summary>
///     Scrapes the search category listing and ensures each observed category exists in the file
///     classification taxonomy, ready to be used for tagging scraped images.
/// </summary>
public sealed class SearchCategoryScrapeAction(ICategoryPageExtractor categoryPageExtractor, IDbContextFactory<AppDbContext> dbContextFactory) : IScrapeAction
{
    /// <inheritdoc />
    public string Name => "Scrape Search Categories";

    /// <inheritdoc />
    public async Task<Exceptional<Unit>> ExecuteAsync(IPage page, CancellationToken token) =>
        await Try.RunAsync(async () =>
        {
            var categoryNames = await categoryPageExtractor.ExtractAsync(page, token);
            await using var context = await dbContextFactory.CreateDbContextAsync(token);
            await context.EnsureCategoriesExistAsync(categoryNames, token);

            return Unit.Instance;
        }, token);
}
