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
    ISearchCategoryWriter searchCategoryWriter,
    IWallpaperCountReader countReader,
    ISearchCategoryReader searchCategoryReader,
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
    private const int ShortDelayForImageSkipInMilliseconds = 125;

    /// <inheritdoc />
    public string Name => "Scrape Search Categories";

    /// <inheritdoc />
    public async Task<Exceptional<Unit>> ExecuteAsync(IPage page, IProgress<string> progress, CancellationToken cancellationToken) =>
        await Try.RunAsync(async () =>
        {
            ScrapeContext scrapeContext = await contextReader.ReadAsync(cancellationToken);

            await scrapeContext.Categories.ForEachAsync(category => VisitCategoryAsync(new CategoryScrapeContext(page, progress, scrapeContext, category, scrapeContext.FileClassifications), cancellationToken));

            return Unit.Instance;
        }, cancellationToken);

    private async Task VisitCategoryAsync(CategoryScrapeContext context, CancellationToken cancellationToken)
    {
        context.Progress.Report($"Visiting category: {context.Category.Name}, searchString: {context.Category.SearchUrl}");
        await context.Page.GotoAsync(context.Category.SearchUrl);

        var wallpaperCount = await countReader.ReadAsync(context.Page, cancellationToken);
        context.Progress.Report($"Number of wallpapers found for category: {context.Category.Name} is {wallpaperCount}");

        var pageCount = (int)Math.Ceiling(wallpaperCount / (double)ImagesPerPage);
        var progressOption = await searchCategoryReader.GetProgressAsync(context.Category.Name, cancellationToken);
        var isFullyVisited = progressOption.MapOrDefault(progress => progress.LastKnownImageCount == wallpaperCount && progress.LastPageVisited == pageCount, false);

        if (isFullyVisited)
        {
            context.Progress.Report($"Category: {context.Category.Name} already fully visited (image count: {wallpaperCount})");
            await Task.Delay(context.ScrapeContext.SearchConfiguration.ImagePauseInSeconds * 1_000, cancellationToken);

            return;
        }

        context.Progress.Report($"Category: {context.Category.Name} has {wallpaperCount} wallpapers, need to get all {pageCount} pages for this category");
        await Enumerable.Range(1, pageCount).ForEachAsync(pageNumber => VisitCategoryPageAsync(context, pageNumber, pageCount, wallpaperCount, cancellationToken));
    }

    private async Task VisitCategoryPageAsync(CategoryScrapeContext context, int pageNumber, int pageCount, int wallpaperCount, CancellationToken cancellationToken)
    {
        SearchCategoryDto searchCategory = new(context.Category.Name, context.Category.IsFamous, context.Category.IsInternet, pageCount, wallpaperCount, pageNumber);
        (await searchCategoryWriter.WriteAsync(searchCategory, cancellationToken)).Match(
            onSuccess: _ => Unit.Instance,
            onFailure: error =>
            {
                context.Progress.Report($"Failed to persist scrape progress for category: {context.Category.Name}, error: {error}");

                return Unit.Instance;
            });

        var pageUrl = $"{context.Category.SearchUrl}&page={pageNumber}";
        context.Progress.Report($"Visiting category: {context.Category.Name}, page {pageNumber} of {pageCount} with searchString: {pageUrl}");
        await context.Page.GotoAsync(pageUrl);

        var hrefs = await hrefCollector.CollectAsync(context.Page, cancellationToken);

        await hrefs.ForEachAsync(href => VisitWallpaperAsync(context, href, cancellationToken));

        await Task.Delay(context.ScrapeContext.SearchConfiguration.ImagePauseInSeconds * 1_000, cancellationToken);
    }

    private async Task VisitWallpaperAsync(CategoryScrapeContext context, string href, CancellationToken cancellationToken)
    {
        var wallpaperId = Path.GetFileName(href);

        if (await fileClassificationRepository.IsAlreadyDownloadedAsync(wallpaperId, cancellationToken))
        {
            context.Progress.Report($"Skipping wallpaper page: {href} as we already have it downloaded");
            await Task.Delay(ShortDelayForImageSkipInMilliseconds, cancellationToken);

            return;
        }

        context.Progress.Report($"Visiting wallpaper page: {href}");
        var response = await context.Page.GotoAsync(href, new PageGotoOptions { Timeout = WallpaperPageTimeoutMilliseconds, });

        if (response is not { Ok: true })
        {
            context.Progress.Report($"Failed to load wallpaper page: {href}, status: {response?.Status}");
            await Task.Delay(context.ScrapeContext.SearchConfiguration.ImagePauseInSeconds * 1_000, cancellationToken);

            return;
        }

        var tags = await tagReader.ReadAsync(context.Page, cancellationToken);
        var curation = TagCurator.Curate(tags, context.ScrapeContext.ModelsToIgnore, context.ScrapeContext.TagsToIgnore);
        var directoryPath = WallpaperDirectoryResolver.Resolve(context.ScrapeContext.Directories, curation.Kept, context.Category, context.FileClassifications);

        var imageUrlOption = await imageLocator.LocateAsync(context.Page, cancellationToken);

        await imageUrlOption.MatchAsync(
            onSomeAsync: imageUrl => DownloadWallpaperAsync(context, new WallpaperDownloadContext(imageUrl, directoryPath, curation.Kept), cancellationToken),
            onNone: () =>
            {
                context.Progress.Report($"Failed to get wallpaper image URL for page: {href}");

                return Unit.Instance;
            });
    }

    private async Task<Unit> DownloadWallpaperAsync(CategoryScrapeContext context, WallpaperDownloadContext download, CancellationToken cancellationToken)
    {
        await categoryRegistrar.EnsureCategoriesExistAsync(download.Tags, cancellationToken);

        var fileName = Path.GetFileName(download.ImageUrl);

        return await (await imageDownloader.DownloadAsync(context.Page, download.ImageUrl, cancellationToken)).MatchAsync(
            onSuccess: async imageBytes =>
            {
                context.Progress.Report($"Downloaded wallpaper image from URL: {download.ImageUrl}, size: {imageBytes.Length} bytes");
                var savedFile = await fileStore.SaveAsync(download.DirectoryPath, fileName, imageBytes, cancellationToken);
                var dimensions = dimensionsReader.Read(imageBytes);

                await fileClassificationRepository.RecordAsync(download.Tags, download.ImageUrl, download.DirectoryPath, savedFile.SizeBytes, dimensions, cancellationToken);
                await Task.Delay(context.ScrapeContext.SearchConfiguration.ImagePauseInSeconds * 1_000, cancellationToken);

                return Unit.Instance;
            },
            onFailure: exception =>
            {
                context.Progress.Report($"Failed to download wallpaper image from URL: {download.ImageUrl}, error: {exception.Message}");

                return Unit.Instance;
            });
    }
}
