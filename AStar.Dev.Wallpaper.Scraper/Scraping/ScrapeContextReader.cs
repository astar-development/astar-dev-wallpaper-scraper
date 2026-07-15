using AStar.Dev.Infrastructure.AppDb;
using Microsoft.EntityFrameworkCore;

namespace AStar.Dev.Wallpaper.Scraper.Scraping;

/// <summary>
///     Reads the one-shot configuration snapshot a scrape run needs, before any Playwright navigation begins.
/// </summary>
public sealed class ScrapeContextReader(IDbContextFactory<AppDbContext> dbContextFactory) : IScrapeContextReader
{
    /// <inheritdoc />
    public async Task<ScrapeContext> ReadAsync(CancellationToken token)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync(token);

        var searchConfiguration = await context.SearchConfigurations.OrderByDescending(configuration => configuration.UpdatedAt).FirstAsync(token);
        var categories = await context.SearchCategories.OrderBy(category => category.Name).ToListAsync(token);
        var modelsToIgnore = await context.ModelsToIgnore.Select(model => model.Value).ToListAsync(token);
        var tagsToIgnore = await context.TagsToIgnore.Select(tag => tag.Value).ToListAsync(token);
        var directories = await context.ScrapeDirectories.OrderByDescending(directory => directory.CreatedAt).FirstOrDefaultAsync(token);

        return new ScrapeContext(
            [.. categories.Select(category => new ScrapeCategory(category.Name, $"{searchConfiguration.SearchStringPrefix}{category.Id}{searchConfiguration.SearchStringSuffix}"))],
            modelsToIgnore,
            tagsToIgnore,
            new DirectoryLayout(directories?.RootDirectory ?? string.Empty, directories?.BaseDirectory ?? string.Empty, directories?.BaseDirectoryFamous ?? string.Empty));
    }
}
