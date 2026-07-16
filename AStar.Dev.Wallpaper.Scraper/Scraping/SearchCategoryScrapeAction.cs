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
    private const int WallpaperPageTimeoutMilliseconds = 30_000;

    /// <inheritdoc />
    public string Name => "Scrape Search Categories";

    /// <inheritdoc />
    public async Task<Exceptional<Unit>> ExecuteAsync(IPage page, IProgress<string> progress, CancellationToken cancellationToken) =>
        await Try.RunAsync(async () =>
        {
            var context = await contextReader.ReadAsync(cancellationToken);

            await context.Categories.ForEachAsync(category => VisitCategoryAsync(page, category, context, progress, cancellationToken));

            return Unit.Instance;
        }, cancellationToken);

    private async Task VisitCategoryAsync(IPage page, ScrapeCategory category, ScrapeContext context, IProgress<string> progress, CancellationToken cancellationToken)
    {
        progress.Report($"Visiting category: {category.Name}, searchString: {category.SearchUrl}");
        await page.GotoAsync(category.SearchUrl);

        var wallpaperCount = await countReader.ReadAsync(page, cancellationToken);
        progress.Report($"Number of wallpapers found for category: {category.Name} is {wallpaperCount}");

        var pageCount = (int)Math.Ceiling(wallpaperCount / (double)ImagesPerPage);
        progress.Report($"Category: {category.Name} has {wallpaperCount} wallpapers, need to get all {pageCount} pages for this category");

        await Enumerable.Range(1, pageCount).ForEachAsync(pageNumber => VisitCategoryPageAsync(page, category, pageNumber, pageCount, context, progress, cancellationToken));
    }

    private async Task VisitCategoryPageAsync(IPage page, ScrapeCategory category, int pageNumber, int pageCount, ScrapeContext context, IProgress<string> progress, CancellationToken cancellationToken)
    {
        var pageUrl = $"{category.SearchUrl}&page={pageNumber}";
        progress.Report($"Visiting category: {category.Name}, page {pageNumber} of {pageCount} with searchString: {pageUrl}");
        await page.GotoAsync(pageUrl);

        var hrefs = await hrefCollector.CollectAsync(page, cancellationToken);
        hrefs.ForEach(href => progress.Report($"Found wallpaper href: {href}"));

        await hrefs.ForEachAsync(href => VisitWallpaperAsync(page, href, context, category, progress, cancellationToken));

        await Task.Delay(context.ImagePauseInSeconds * 1_000, cancellationToken);
    }

    private async Task VisitWallpaperAsync(IPage page, string href, ScrapeContext context, ScrapeCategory category, IProgress<string> progress, CancellationToken cancellationToken)
    {
        progress.Report($"Visiting wallpaper page: {href}");
        var response = await page.GotoAsync(href, new PageGotoOptions { Timeout = WallpaperPageTimeoutMilliseconds, });

        if (response is not { Ok: true })
        {
            progress.Report($"Failed to load wallpaper page: {href}, status: {response?.Status}");
            await Task.Delay(context.ImagePauseInSeconds * 1_000, cancellationToken);

            return;
        }

        var tags = await tagReader.ReadAsync(page, cancellationToken);
        var curation = TagCurator.Curate(tags, context.ModelsToIgnore, context.TagsToIgnore);

        var (isAlreadyDownloaded, directoryPath) = await CheckWhetherFileIsAlreadyDownloadedAsync(href, context, progress, curation.Kept, category, cancellationToken);
        if (isAlreadyDownloaded) return;

        var imageUrlOption = await imageLocator.LocateAsync(page, cancellationToken);

        await imageUrlOption.MatchAsync(
            onSomeAsync: imageUrl => DownloadWallpaperAsync(page, directoryPath, imageUrl, curation.Kept, context.Directories, context, progress, category, cancellationToken),
            onNone: () =>
            {
                progress.Report($"Failed to get wallpaper image URL for page: {href}");

                return Unit.Instance;
            });
    }

    private async Task<(bool, string)> CheckWhetherFileIsAlreadyDownloadedAsync(string href, ScrapeContext context, IProgress<string> progress, IReadOnlyList<TagData> tags, ScrapeCategory category, CancellationToken cancellationToken)
    {
        var directoryPath = WallpaperDirectoryResolver.Resolve(context.Directories, tags, category);
        var wallpaperId = Path.GetFileName(href);

        if (await fileClassificationRepository.IsAlreadyDownloadedAsync(directoryPath, wallpaperId, cancellationToken))
        {
            progress.Report($"Skipping wallpaper page: {href} as we already have it downloaded in directory: {directoryPath}");
            await Task.Delay(context.ImagePauseInSeconds * 1_000, cancellationToken);
            return (true, directoryPath);
        }

        return (false, directoryPath);
    }

    private async Task<Unit> DownloadWallpaperAsync(IPage page, string directoryPath, string imageUrl, IReadOnlyList<TagData> tags, DirectoryLayout directories, ScrapeContext context, IProgress<string> progress, ScrapeCategory category, CancellationToken cancellationToken)
    {
        await categoryRegistrar.EnsureCategoriesExistAsync(tags, cancellationToken);

        var fileName = Path.GetFileName(imageUrl);

        var imageBytes = await imageDownloader.DownloadAsync(page, imageUrl, cancellationToken);
        progress.Report($"Downloaded wallpaper image from URL: {imageUrl}, size: {imageBytes.Length} bytes");
        var savedFile = await fileStore.SaveAsync(directoryPath, fileName, imageBytes, cancellationToken);
        var dimensions = dimensionsReader.Read(imageBytes);

        await fileClassificationRepository.RecordAsync(tags, imageUrl, directoryPath, savedFile.SizeBytes, dimensions, cancellationToken);
        await Task.Delay(context.ImagePauseInSeconds * 1_000, cancellationToken);

        return Unit.Instance;
    }
}
