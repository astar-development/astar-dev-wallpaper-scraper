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
            var modelsToIgnore = await context.ModelsToIgnore.ToListAsync(token);
            var tagsToIgnore = await context.TagsToIgnore.ToListAsync(token);

            var categories = await context.SearchCategories.OrderBy(c => c.Name).ToListAsync(token);

            List<(string, string, bool, List<string?>)> pagesToVisit = [.. categories.Select(c => (c.Name, $"{searchPrefix}{c.Id}{searchSuffix}", false, new List<string?>()))];

            foreach (var (name, urlToVisit, isVisited, hrefs) in pagesToVisit)
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
                var numberOfPages = (int)Math.Ceiling(wallpapersFound / (double)ImagesPerPage);
                progress.Report($"Category: {name} has {wallpapersFound} wallpapers, need to get all {numberOfPages} pages for this category");

                for (var pageNumber = 1; pageNumber <= numberOfPages; pageNumber++)
                {
                    var pageUrl = $"{urlToVisit}&page={pageNumber}";
                    progress.Report($"Visiting category: {name}, page {pageNumber} of {numberOfPages} with searchString: {pageUrl}");
                    response = await page.GotoAsync(pageUrl);

                    pagesToVisit = await ProcessPageAsync(pagesToVisit, name, progress, modelsToIgnore, tagsToIgnore, token);
                }
            }

            return Unit.Instance;
        }, token);

    private async Task<List<(string, string, bool, List<string?>)>> ProcessPageAsync(List<(string, string, bool, List<string?>)> pagesToVisit, string name, IProgress<string> progress, List<ModelToIgnoreEntity> modelsToIgnore, List<TagToIgnoreEntity> tagsToIgnore, CancellationToken token)
    {
        var imagePreviews = await ImagePreviews.AllAsync().ConfigureAwait(false);
        List<string?> hrefs = [];
        foreach (var imagePreview in imagePreviews)
        {
            var href = await imagePreview.GetAttributeAsync("href").ConfigureAwait(false);
            if (href?.StartsWith("https://wallhaven.cc/w/", StringComparison.InvariantCultureIgnoreCase) == true)
            {
                progress.Report($"Found wallpaper href: {href}");
                hrefs.Add(href);
            }
        }
        await ProcessImagePageAsync(hrefs, progress, modelsToIgnore, tagsToIgnore, token);

        await Task.Delay(PageDelayMilliseconds, token);
        pagesToVisit = MarkPageVisited(pagesToVisit, name, hrefs);

        return pagesToVisit;
    }

    private async Task ProcessImagePageAsync(List<string?> hrefs, IProgress<string> progress, List<ModelToIgnoreEntity> modelsToIgnore, List<TagToIgnoreEntity> tagsToIgnore, CancellationToken token)
    {
        var context = await dbContextFactory.CreateDbContextAsync(token);
        var directories = context.ScrapeDirectories.OrderByDescending(d => d.CreatedAt).FirstOrDefault();

        foreach (var href in hrefs)
        {
            if (href is null)
            {
                continue;
            }

            progress.Report($"Visiting wallpaper page: {href}");
            var response = await page.GotoAsync(href, new PageGotoOptions { Timeout = 30_000, });
            if (!response?.Ok ?? true)
            {
                progress.Report($"Failed to load wallpaper page: {href}, status: {response?.Status}");
                continue;
            }

            var tagLocators = await page.Locator(".tagname").AllAsync().ConfigureAwait(false);
            var tagData = await Task.WhenAll(tagLocators.Select(GetTagsAsync)).ConfigureAwait(false);
            List<TagData> tagList = BuildTagList(progress, modelsToIgnore, tagsToIgnore, tagData);

            var imageTag = page.Locator("#wallpaper");
            var imageUrl = await imageTag.GetAttributeAsync("src").ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(imageUrl))
            {
                progress.Report($"Failed to get wallpaper image URL for page: {href}");
                continue;
            }

            await AddNewCategoriesAsync(context, tagList, token);

            var directoryPath = DirectoryHelper(directories, tagList);
            var fileName = Path.GetFileName(imageUrl);
            if (await ImageAlreadyDownloaded(context, directoryPath, fileName, token)) continue;

            var imagePage = await page.GotoAsync(imageUrl, new PageGotoOptions { Timeout = 30_000, }).ConfigureAwait(false);
            var image = await imagePage?.BodyAsync()!;
            var disposableImage = SkiaSharp.SKImage.FromEncodedData(image);
            var fullPath = Path.Combine(directoryPath, fileName);
#pragma warning disable CS8602 // Dereference of a possibly null reference.
            new FileInfo(fullPath).Directory.Create();
#pragma warning restore CS8602 // Dereference of a possibly null reference.
            // we need to save the categories for the image to the database, so we can use them for tagging later
            File.WriteAllBytes(fullPath, image);
            var fileInfo = new FileInfo(fullPath);
            await AddFileClassifications(context, tagList, imageUrl, directoryPath, disposableImage, fileInfo, token);
            await Task.Delay(PageDelayMilliseconds, token);
        }
    }

    private static async Task AddFileClassifications(AppDbContext context, List<TagData> tagList, string imageUrl, string directoryPath, SkiaSharp.SKImage disposableImage, FileInfo fileInfo, CancellationToken token)
    {
        foreach (var tag in tagList)
        {
            var fileClassification = new FileClassificationEntity
            {
                FileDetailId = new FileId(Guid.CreateVersion7()),
                CategoryId = context.FileClassificationCategories.First(c => c.Name == tag.Tag).Id,
                FileDetail = new FileDetailEntity
                {
                    FileName = new FileName(Path.GetFileName(imageUrl)),
                    DirectoryName = new DirectoryName(directoryPath),
                    FileHandle = new FileHandle(Guid.NewGuid().ToString()),
                    FileSize = fileInfo.Length,
                    IsImage = true,
                    ImageDetail = new ImageDetailEntity
                    {
                        Width = disposableImage.Width,
                        Height = disposableImage.Height,
                    },
                },
            };
            context.FileClassifications.Add(fileClassification);
        }

        await context.SaveChangesAsync(token);
    }

    private static async Task<bool> ImageAlreadyDownloaded(AppDbContext context, string directoryPath, string fileName, CancellationToken token)
    {
        return await context.Files.AnyAsync(f => f.FileName.Value == fileName && f.DirectoryName.Value == directoryPath, token);
    }

    private static List<TagData> BuildTagList(IProgress<string> progress, List<ModelToIgnoreEntity> modelsToIgnore, List<TagToIgnoreEntity> tagsToIgnore, TagData[] tagData)
    {
        List<TagData> tagList = [];
        foreach (var tag in tagData)
        {
            if (tag is null)
            {
                continue;
            }
            else
            {
                progress.Report($"Found tag: {tag.Tag}, category: {tag.Category}, isFamous: {tag.IsFamous}, isInternet: {tag.IsInternet}");
            }

            if (modelsToIgnore.Any(m => m.Value.Equals(tag.Tag, StringComparison.OrdinalIgnoreCase)))
            {
                progress.Report($"Ignoring model: {tag.Tag} as it is in the modelsToIgnore list");
                continue;
            }
            else
            {
                progress.Report($"Model: {tag.Tag} is not in the modelsToIgnore list, we should save it to the database");
            }

            if (tagsToIgnore.Any(t => t.Value.Equals(tag.Tag, StringComparison.OrdinalIgnoreCase)))
            {
                progress.Report($"Ignoring tag: {tag.Tag} as it is in the tagsToIgnore list");
                continue;
            }
            else
            {
                tagList.Add(tag);
                progress.Report($"Tag: {tag.Tag} is not in the tagsToIgnore list, added to the list of tags to save to the database");
            }
        }

        return tagList;
    }

    private static async Task AddNewCategoriesAsync(AppDbContext context, List<TagData> tagList, CancellationToken token)
    {
        foreach (var tag2 in tagList.Where(t => !string.IsNullOrWhiteSpace(t.Category) && !string.IsNullOrWhiteSpace(t.Tag)))
        {
            if (!await context.FileClassificationCategories.AnyAsync(t => t.Name == tag2.Tag, token))
            {
                context.FileClassificationCategories.Add(new FileClassificationCategoryEntity
                {
                    Name = tag2.Tag,
                    IsFamous = tag2.IsFamous,
                    IsInternet = tag2.IsInternet,
                    IncludeInSearch = true,
                });
            }
        }

            await context.SaveChangesAsync(token);
    }

    private static string DirectoryHelper(ScrapeDirectoriesEntity? directories, List<TagData> tags)
    {
        var baseDirectory = directories?.RootDirectory ?? string.Empty;
        if (tags.Any(t => t.IsFamous))
        {
            baseDirectory += directories?.BaseDirectoryFamous ?? string.Empty;
        }
        else
        {
            baseDirectory += directories?.BaseDirectory ?? string.Empty;
        }

        foreach (var tag in tags.Where(t => t.IsFamous || t.IsInternet))
        {
            if (tag.Category is null)
            {
                continue;
            }

            baseDirectory = Path.Combine(baseDirectory, tag.Tag);
        }

        foreach (var tag in tags.Where(t => !t.IsFamous && !t.IsInternet))
        {
            if (tag.Category is null)
            {
                continue;
            }

            baseDirectory = Path.Combine(baseDirectory, tag.Tag);
        }

        return baseDirectory;
    }

    private static List<(string, string, bool, List<string?>)> MarkPageVisited(List<(string, string, bool, List<string?>)> pagesToVisit, string name, List<string?> hrefs)
        => [.. pagesToVisit.Select(p => p.Item1 == name ? (p.Item1, p.Item2, true, hrefs) : p)];

    private static async Task<TagData> GetTagsAsync(ILocator tag)
    {
        string textTask = await tag.InnerTextAsync().ConfigureAwait(false);
        string? attrTask = await tag.GetAttributeAsync("original-title").ConfigureAwait(false);

        return new TagData(textTask, attrTask);
    }
}
