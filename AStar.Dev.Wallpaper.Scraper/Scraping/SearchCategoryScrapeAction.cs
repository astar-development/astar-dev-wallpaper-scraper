using AStar.Dev.FunctionalParadigm;
using AStar.Dev.Infrastructure.AppDb;
using AStar.Dev.Infrastructure.AppDb.Entities;
using AStar.Dev.Utilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Playwright;

namespace AStar.Dev.Wallpaper.Scraper.Scraping;

/// <summary>
///     Scrapes the search category listing and ensures each observed category exists in the file
///     classification taxonomy, ready to be used for tagging scraped images.
/// </summary>
public sealed class SearchCategoryScrapeAction(/*ICategoryPageExtractor categoryPageExtractor,*/ IDbContextFactory<AppDbContext> dbContextFactory) : IScrapeAction
{
    private const int ImagesPerPage = 24;
    private const int PageDelayMilliseconds = 2_000;
    private IPage page = null!;
    private ILocator ImagePreviews => page.GetByRole(AriaRole.Link);
    private ILocator WallpaperSearchHeader => page.GetByText("Wallpapers found for", new PageGetByTextOptions { Exact = false, });

    /// <inheritdoc />
    public string Name => "Scrape Search Categories";

    /// <inheritdoc />
    public async Task<Exceptional<Unit>> ExecuteAsync(IPage page, IProgress<string> progress, CancellationToken token) =>
        await Try.RunAsync(async () =>
        {
            this.page = page;
            await using var context = await dbContextFactory.CreateDbContextAsync(token);
            var searchConfiguration = await context.SearchConfigurations.OrderByDescending(s => s.UpdatedAt).FirstAsync(token);
            var searchPrefix = searchConfiguration.SearchStringPrefix;
            var searchSuffix = searchConfiguration.SearchStringSuffix;

            var categories = await context.SearchCategories.OrderBy(c => c.Name).ToListAsync(token);

            List<(string,string,bool)> pagesToVisit = [.. categories.Select(c => (c.Name, $"{searchPrefix}{c.Id}{searchSuffix}", false))];

            foreach (var (name, urlToVisit, isVisited) in pagesToVisit)
            {
                if (isVisited)
                {
                    continue;
                }

                progress.Report($"Visiting category: {name}, searchString: {urlToVisit}");
                var response = await page.GotoAsync(urlToVisit);
                var headerText = await WallpaperSearchHeader.AllTextContentsAsync();
                var wallpapersFound = headerText[0]?.Split(" ").FirstOrDefault()?.ToIntSafe() ?? 0;
                progress.Report($"Number of wallpapers found for category: {name} is {wallpapersFound}");
                if(wallpapersFound > ImagesPerPage)
                {
                    var numberOfPages = (int)Math.Ceiling(wallpapersFound / (double)ImagesPerPage);
                    progress.Report($"Category: {name} has {wallpapersFound} wallpapers, need to get all {numberOfPages} pages for this category");
                }
                else
                {
                    pagesToVisit = await ProcessPage(pagesToVisit, name);
                }
            }

            // var categoryNames = await categoryPageExtractor.ExtractAsync(page, token);
            // await context.EnsureCategoriesExistAsync(categoryNames, token);

            return Unit.Instance;
        }, token);

    private async Task<List<(string, string, bool)>> ProcessPage(List<(string, string, bool)> pagesToVisit, string name)
    {
        var imagePreviews = await ImagePreviews.AllAsync().ConfigureAwait(false);
        List<string?> hrefs = [];
        foreach (var imagePreview in imagePreviews)
        {
            var href = await imagePreview.GetAttributeAsync("href").ConfigureAwait(false);
            if (href?.StartsWith("https://wallhaven.cc/w/", StringComparison.InvariantCultureIgnoreCase) == true)
            {
                hrefs.Add(href);
            }
        }

        await Task.Delay(PageDelayMilliseconds);
        pagesToVisit = MarkPageVisited(pagesToVisit, name);
        return pagesToVisit;
    }

    private static List<(string, string, bool)> MarkPageVisited(List<(string, string, bool)> pagesToVisit, string name)
    {

        // Mark this page as visited
        pagesToVisit = pagesToVisit.Select(p => p.Item1 == name ? (p.Item1, p.Item2, true) : p).ToList();
        return pagesToVisit;
    }
}
