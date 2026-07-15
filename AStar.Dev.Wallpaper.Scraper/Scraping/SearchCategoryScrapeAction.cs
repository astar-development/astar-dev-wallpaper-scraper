using AStar.Dev.FunctionalParadigm;
using AStar.Dev.Utilities;
using Microsoft.Playwright;

namespace AStar.Dev.Wallpaper.Scraper.Scraping;

/// <summary>
///     Scrapes the search category listing and ensures each observed category exists in the file
///     classification taxonomy, ready to be used for tagging scraped images.
/// </summary>
public sealed class SearchCategoryScrapeAction(
    IScrapeContextReader contextReader,
    IWallpaperCountReader countReader,
    IWallpaperHrefCollector hrefCollector,
    ITagReader tagReader,
    IWallpaperImageLocator imageLocator,
    IWallpaperImageDownloader imageDownloader,
    IImageDimensionsReader dimensionsReader,
    IWallpaperFileStore fileStore,
    IWallpaperCategoryRegistrar categoryRegistrar,
    IWallpaperFileClassificationRepository fileClassificationRepository) : IScrapeAction
{
    private const int ImagesPerPage = 24;
    private const int PageDelayMilliseconds = 2_000;
    private const int WallpaperPageTimeoutMilliseconds = 30_000;

    /// <inheritdoc />
    public string Name => "Scrape Search Categories";

    /// <inheritdoc />
    public async Task<Exceptional<Unit>> ExecuteAsync(IPage page, IProgress<string> progress, CancellationToken token) =>
        await Try.RunAsync(async () =>
        {
            var context = await contextReader.ReadAsync(token);

            await context.Categories.ForEachAsync(category => VisitCategoryAsync(page, category, context, progress, token));

            return Unit.Instance;
        }, token);

    private async Task VisitCategoryAsync(IPage page, ScrapeCategory category, ScrapeContext context, IProgress<string> progress, CancellationToken token)
    {
        progress.Report($"Visiting category: {category.Name}, searchString: {category.SearchUrl}");
        await page.GotoAsync(category.SearchUrl);

        var wallpaperCount = await countReader.ReadAsync(page, token);
        progress.Report($"Number of wallpapers found for category: {category.Name} is {wallpaperCount}");

        var pageCount = (int)Math.Ceiling(wallpaperCount / (double)ImagesPerPage);
        progress.Report($"Category: {category.Name} has {wallpaperCount} wallpapers, need to get all {pageCount} pages for this category");

        await Enumerable.Range(1, pageCount).ForEachAsync(pageNumber => VisitCategoryPageAsync(page, category, pageNumber, pageCount, context, progress, token));
    }

    private async Task VisitCategoryPageAsync(IPage page, ScrapeCategory category, int pageNumber, int pageCount, ScrapeContext context, IProgress<string> progress, CancellationToken token)
    {
        var pageUrl = $"{category.SearchUrl}&page={pageNumber}";
        progress.Report($"Visiting category: {category.Name}, page {pageNumber} of {pageCount} with searchString: {pageUrl}");
        await page.GotoAsync(pageUrl);

        var hrefs = await hrefCollector.CollectAsync(page, token);
        hrefs.ForEach(href => progress.Report($"Found wallpaper href: {href}"));

        await hrefs.ForEachAsync(href => VisitWallpaperAsync(page, href, context, progress, token));

        await Task.Delay(PageDelayMilliseconds, token);
    }

    private async Task VisitWallpaperAsync(IPage page, string href, ScrapeContext context, IProgress<string> progress, CancellationToken token)
    {
        progress.Report($"Visiting wallpaper page: {href}");
        var response = await page.GotoAsync(href, new PageGotoOptions { Timeout = WallpaperPageTimeoutMilliseconds, });

        if (response is not { Ok: true })
        {
            progress.Report($"Failed to load wallpaper page: {href}, status: {response?.Status}");

            return;
        }

        var tags = await tagReader.ReadAsync(page, token);
        var curation = TagCurator.Curate(tags, context.ModelsToIgnore, context.TagsToIgnore);
        curation.Messages.ForEach(progress.Report);

        var imageUrlOption = await imageLocator.LocateAsync(page, token);

        await imageUrlOption.MatchAsync(
            onSomeAsync: imageUrl => DownloadWallpaperAsync(page, imageUrl, curation.Kept, context.Directories, token),
            onNone: () =>
            {
                progress.Report($"Failed to get wallpaper image URL for page: {href}");

                return Unit.Instance;
            });
    }

    private async Task<Unit> DownloadWallpaperAsync(IPage page, string imageUrl, IReadOnlyList<TagData> tags, DirectoryLayout directories, CancellationToken token)
    {
        await categoryRegistrar.EnsureCategoriesExistAsync(tags, token);

        var directoryPath = WallpaperDirectoryResolver.Resolve(directories, tags);
        var fileName = Path.GetFileName(imageUrl);

        if (await fileClassificationRepository.IsAlreadyDownloadedAsync(directoryPath, fileName, token))
            return Unit.Instance;

        var imageBytes = await imageDownloader.DownloadAsync(page, imageUrl, token);
        var savedFile = await fileStore.SaveAsync(directoryPath, fileName, imageBytes, token);
        var dimensions = dimensionsReader.Read(imageBytes);

        await fileClassificationRepository.RecordAsync(tags, imageUrl, directoryPath, savedFile.SizeBytes, dimensions, token);
        await Task.Delay(PageDelayMilliseconds, token);

        return Unit.Instance;
    }
}
