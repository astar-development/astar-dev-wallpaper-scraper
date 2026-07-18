using AStar.Dev.FunctionalParadigm;
using AStar.Dev.Infrastructure.AppDb.Entities;
using AStar.Dev.Wallpaper.Scraper.Scraping;
using Microsoft.Playwright;

namespace AStar.Dev.Wallpaper.Scraper.Tests.Unit.Scraping;

public sealed class GivenSearchCategoryScrapeAction
{
    private readonly IScrapeContextReader contextReader = Substitute.For<IScrapeContextReader>();
    private readonly ISearchCategoryWriter categoryWriter = Substitute.For<ISearchCategoryWriter>();
    private readonly IWallpaperCountReader countReader = Substitute.For<IWallpaperCountReader>();
    private readonly IWallpaperHrefCollector hrefCollector = Substitute.For<IWallpaperHrefCollector>();
    private readonly ITagReader tagReader = Substitute.For<ITagReader>();
    private readonly IWallpaperImageLocator imageLocator = Substitute.For<IWallpaperImageLocator>();
    private readonly IWallpaperImageDownloader imageDownloader = Substitute.For<IWallpaperImageDownloader>();
    private readonly IImageDimensionsReader dimensionsReader = Substitute.For<IImageDimensionsReader>();
    private readonly IWallpaperFileStore fileStore = Substitute.For<IWallpaperFileStore>();
    private readonly IWallpaperCategoryRegistrar categoryRegistrar = Substitute.For<IWallpaperCategoryRegistrar>();
    private readonly IWallpaperFileClassificationRepository fileClassificationRepository = Substitute.For<IWallpaperFileClassificationRepository>();
    private readonly IProgress<string> progress = Substitute.For<IProgress<string>>();
    private readonly IPage page = Substitute.For<IPage>();

    public GivenSearchCategoryScrapeAction() =>
        categoryWriter.WriteAsync(Arg.Any<SearchCategoryDto>(), Arg.Any<CancellationToken>()).Returns(Result.Success<FunctionalParadigm.Unit, string>(FunctionalParadigm.Unit.Instance));

    private static readonly ScrapeContext singleCategoryContext = new(
        [new ScrapeCategory("Nature", "https://wallhaven.cc/search?categories=1", false, false)],
        [],
        [],
        new DirectoryLayout("/root", "/base", "/famous"), [], new SearchConfigurationEntity
        {
            Id = 1,
            SearchStringPrefix = "https://wallhaven.cc/search?categories=",
            SearchStringSuffix = string.Empty,
            ImagePauseInSeconds = 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });

    private static readonly ScrapeContext twoCategoryContext = new(
        [
            new ScrapeCategory("Nature", "https://wallhaven.cc/search?categories=1", false, false), 
            new ScrapeCategory("Space", "https://wallhaven.cc/search?categories=2", false, false),
        ],
        [],
        [],
        new DirectoryLayout("/root", "/base", "/famous"), [], new SearchConfigurationEntity
        {
            Id = 1,
            SearchStringPrefix = "https://wallhaven.cc/search?categories=",
            SearchStringSuffix = string.Empty,
            ImagePauseInSeconds = 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });

    private static readonly ScrapeContext ignoredTagContext = new(
        [new ScrapeCategory("Nature", "https://wallhaven.cc/search?categories=1", false, false)],
        [],
        ["Ignored"],
        new DirectoryLayout("/root", "/base", "/famous"), [], new SearchConfigurationEntity
        {
            Id = 1,
            SearchStringPrefix = "https://wallhaven.cc/search?categories=",
            SearchStringSuffix = string.Empty,
            ImagePauseInSeconds = 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });

    [Fact]
    public async Task when_a_category_has_more_wallpapers_than_fit_on_one_page_then_progress_reports_the_page_count_and_a_success_result_is_returned()
    {
        contextReader.ReadAsync(Arg.Any<CancellationToken>()).Returns(singleCategoryContext);
        countReader.ReadAsync(page, Arg.Any<CancellationToken>()).Returns(50);
        var sut = CreateSut();

        var result = await sut.ExecuteAsync(page, progress, TestContext.Current.CancellationToken);

        result.ShouldBeOfType<Success<FunctionalParadigm.Unit>>();
        progress.Received().Report(Arg.Is<string>(message => message!.Contains("Visiting category: Nature")));
        progress.Received().Report(Arg.Is<string>(message => message!.Contains("need to get all 3 pages")));
    }

    [Fact]
    public async Task when_persisting_scrape_progress_fails_then_progress_reports_the_failure_and_the_page_is_still_visited()
    {
        contextReader.ReadAsync(Arg.Any<CancellationToken>()).Returns(singleCategoryContext);
        countReader.ReadAsync(page, Arg.Any<CancellationToken>()).Returns(1);
        categoryWriter.WriteAsync(Arg.Any<SearchCategoryDto>(), Arg.Any<CancellationToken>()).Returns(Result.Failure<FunctionalParadigm.Unit, string>("No search category named 'Nature' exists to update."));
        var sut = CreateSut();

        var result = await sut.ExecuteAsync(page, progress, TestContext.Current.CancellationToken);

        result.ShouldBeOfType<Success<FunctionalParadigm.Unit>>();
        progress.Received().Report(Arg.Is<string>(message => message!.Contains("Failed to persist scrape progress for category: Nature") && message!.Contains("No search category named 'Nature' exists to update.")));
        await page.Received().GotoAsync(Arg.Is<string>(url => url!.Contains("&page=1")));
    }

    [Fact]
    public async Task when_multiple_categories_are_configured_then_each_category_is_visited_on_its_own_search_url()
    {
        contextReader.ReadAsync(Arg.Any<CancellationToken>()).Returns(twoCategoryContext);
        countReader.ReadAsync(page, Arg.Any<CancellationToken>()).Returns(1);
        hrefCollector.CollectAsync(page, Arg.Any<CancellationToken>()).Returns([]);
        var sut = CreateSut();

        var result = await sut.ExecuteAsync(page, progress, TestContext.Current.CancellationToken);

        result.ShouldBeOfType<Success<FunctionalParadigm.Unit>>();
        progress.Received().Report(Arg.Is<string>(message => message!.Contains("Visiting category: Nature")));
        progress.Received().Report(Arg.Is<string>(message => message!.Contains("Visiting category: Space")));
        await page.Received(1).GotoAsync("https://wallhaven.cc/search?categories=1&page=1");
        await page.Received(1).GotoAsync("https://wallhaven.cc/search?categories=2&page=1");
        await hrefCollector.Received(2).CollectAsync(page, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_a_category_spans_multiple_pages_then_each_page_is_visited_and_hrefs_are_collected_per_page()
    {
        contextReader.ReadAsync(Arg.Any<CancellationToken>()).Returns(singleCategoryContext);
        countReader.ReadAsync(page, Arg.Any<CancellationToken>()).Returns(30);
        hrefCollector.CollectAsync(page, Arg.Any<CancellationToken>()).Returns([]);
        var sut = CreateSut();

        var result = await sut.ExecuteAsync(page, progress, TestContext.Current.CancellationToken);

        result.ShouldBeOfType<Success<FunctionalParadigm.Unit>>();
        await page.Received(1).GotoAsync("https://wallhaven.cc/search?categories=1&page=1");
        await page.Received(1).GotoAsync("https://wallhaven.cc/search?categories=1&page=2");
        await hrefCollector.Received(2).CollectAsync(page, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_reading_the_scrape_context_fails_then_a_failure_result_is_returned_instead_of_throwing()
    {
        contextReader.ReadAsync(Arg.Any<CancellationToken>()).Returns<ScrapeContext>(_ => throw new InvalidOperationException("Sequence contains no elements"));
        var sut = CreateSut();

        var result = await sut.ExecuteAsync(page, progress, TestContext.Current.CancellationToken);

        result.ShouldBeOfType<Failure<FunctionalParadigm.Unit>>();
    }

    [Fact]
    public async Task when_the_wallpaper_page_fails_to_load_then_progress_reports_the_failure_and_no_tags_are_read()
    {
        contextReader.ReadAsync(Arg.Any<CancellationToken>()).Returns(singleCategoryContext);
        countReader.ReadAsync(page, Arg.Any<CancellationToken>()).Returns(1);
        hrefCollector.CollectAsync(page, Arg.Any<CancellationToken>()).Returns(["https://wallhaven.cc/w/abc123"]);
        var failedResponse = Substitute.For<IResponse>();
        failedResponse.Ok.Returns(false);
        failedResponse.Status.Returns(404);
        page.GotoAsync("https://wallhaven.cc/w/abc123", Arg.Any<PageGotoOptions>()).Returns(failedResponse);
        var sut = CreateSut();

        var result = await sut.ExecuteAsync(page, progress, TestContext.Current.CancellationToken);

        result.ShouldBeOfType<Success<FunctionalParadigm.Unit>>();
        progress.Received().Report(Arg.Is<string>(message => message!.Contains("Failed to load wallpaper page")));
        await tagReader.DidNotReceive().ReadAsync(Arg.Any<IPage>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_the_wallpaper_has_no_image_url_then_progress_reports_the_failure_and_nothing_is_downloaded()
    {
        contextReader.ReadAsync(Arg.Any<CancellationToken>()).Returns(singleCategoryContext);
        countReader.ReadAsync(page, Arg.Any<CancellationToken>()).Returns(1);
        hrefCollector.CollectAsync(page, Arg.Any<CancellationToken>()).Returns(["https://wallhaven.cc/w/abc123"]);
        var okResponse = Substitute.For<IResponse>();
        okResponse.Ok.Returns(true);
        page.GotoAsync("https://wallhaven.cc/w/abc123", Arg.Any<PageGotoOptions>()).Returns(okResponse);
        tagReader.ReadAsync(page, Arg.Any<CancellationToken>()).Returns([new TagData("Nature", "outdoors")]);
        imageLocator.LocateAsync(page, Arg.Any<CancellationToken>()).Returns(Option<string>.None.Instance);
        var sut = CreateSut();

        var result = await sut.ExecuteAsync(page, progress, TestContext.Current.CancellationToken);

        result.ShouldBeOfType<Success<FunctionalParadigm.Unit>>();
        progress.Received().Report(Arg.Is<string>(message => message!.Contains("Failed to get wallpaper image URL")));
        await imageDownloader.DidNotReceive().DownloadAsync(Arg.Any<IPage>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_the_wallpaper_was_already_downloaded_then_its_page_is_never_visited_and_it_is_not_downloaded_again()
    {
        ConfigureSuccessfulWallpaperVisit();
        fileClassificationRepository.IsAlreadyDownloadedAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(true);
        var sut = CreateSut();

        var result = await sut.ExecuteAsync(page, progress, TestContext.Current.CancellationToken);

        result.ShouldBeOfType<Success<FunctionalParadigm.Unit>>();
        await page.DidNotReceive().GotoAsync("https://wallhaven.cc/w/abc123", Arg.Any<PageGotoOptions>());
        await tagReader.DidNotReceive().ReadAsync(Arg.Any<IPage>(), Arg.Any<CancellationToken>());
        await imageDownloader.DidNotReceive().DownloadAsync(Arg.Any<IPage>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        await fileStore.DidNotReceive().SaveAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<byte[]>(), Arg.Any<CancellationToken>());
        await fileClassificationRepository.DidNotReceive().RecordAsync(Arg.Any<IReadOnlyList<TagData>>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<long>(), Arg.Any<ImageDimensions>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_checking_whether_a_wallpaper_is_already_downloaded_then_the_check_is_a_contains_match_on_the_wallpaper_id()
    {
        ConfigureSuccessfulWallpaperVisit();
        fileClassificationRepository.IsAlreadyDownloadedAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);
        imageLocator.LocateAsync(page, Arg.Any<CancellationToken>()).Returns(Option<string>.None.Instance);
        var sut = CreateSut();

        await sut.ExecuteAsync(page, progress, TestContext.Current.CancellationToken);

        await fileClassificationRepository.Received().IsAlreadyDownloadedAsync("abc123", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_a_wallpaper_has_a_tag_on_the_ignore_list_then_it_is_saved_under_the_curated_directory_path()
    {
        contextReader.ReadAsync(Arg.Any<CancellationToken>()).Returns(ignoredTagContext);
        countReader.ReadAsync(page, Arg.Any<CancellationToken>()).Returns(1);
        hrefCollector.CollectAsync(page, Arg.Any<CancellationToken>()).Returns(["https://wallhaven.cc/w/abc123"]);
        var okResponse = Substitute.For<IResponse>();
        okResponse.Ok.Returns(true);
        page.GotoAsync("https://wallhaven.cc/w/abc123", Arg.Any<PageGotoOptions>()).Returns(okResponse);
        tagReader.ReadAsync(page, Arg.Any<CancellationToken>())
            .Returns([new TagData("Nature", "outdoors"), new TagData("Ignored", "outdoors")]);
        imageLocator.LocateAsync(page, Arg.Any<CancellationToken>()).Returns(new Option<string>.Some("https://wallhaven.cc/images/pic.jpg"));
        byte[] imageBytes = [1, 2, 3];
        imageDownloader.DownloadAsync(page, "https://wallhaven.cc/images/pic.jpg", Arg.Any<CancellationToken>()).Returns(Exceptional.Success(imageBytes));
        fileStore.SaveAsync(Arg.Any<string>(), "pic.jpg", imageBytes, Arg.Any<CancellationToken>()).Returns(new SavedWallpaperFile("/root/base/N/Nature/pic.jpg", 3));
        var sut = CreateSut();

        await sut.ExecuteAsync(page, progress, TestContext.Current.CancellationToken);

        await fileStore.Received().SaveAsync("/root/base/N/Nature", "pic.jpg", imageBytes, Arg.Any<CancellationToken>());
        await fileStore.DidNotReceive().SaveAsync("/root/base/N/Nature/Ignored", Arg.Any<string>(), Arg.Any<byte[]>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_a_new_wallpaper_is_visited_then_its_categories_are_registered_and_it_is_downloaded_saved_and_recorded()
    {
        ConfigureSuccessfulWallpaperVisit();
        fileClassificationRepository.IsAlreadyDownloadedAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);
        byte[] imageBytes = [1, 2, 3];
        imageDownloader.DownloadAsync(page, "https://wallhaven.cc/images/pic.jpg", Arg.Any<CancellationToken>()).Returns(Exceptional.Success(imageBytes));
        var savedFile = new SavedWallpaperFile("/root/base/pic.jpg", 3);
        fileStore.SaveAsync(Arg.Any<string>(), "pic.jpg", imageBytes, Arg.Any<CancellationToken>()).Returns(savedFile);
        var dimensions = new ImageDimensions(10, 20);
        dimensionsReader.Read(imageBytes).Returns(dimensions);
        var sut = CreateSut();

        var result = await sut.ExecuteAsync(page, progress, TestContext.Current.CancellationToken);

        result.ShouldBeOfType<Success<FunctionalParadigm.Unit>>();
        await categoryRegistrar.Received().EnsureCategoriesExistAsync(Arg.Is<IReadOnlyList<TagData>>(tags => tags != null && tags.Any(tag => tag.Tag == "Nature")), Arg.Any<CancellationToken>());
        await fileClassificationRepository.Received().RecordAsync(Arg.Any<IReadOnlyList<TagData>>(), "https://wallhaven.cc/images/pic.jpg", Arg.Any<string>(), 3, dimensions, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_the_image_download_fails_then_progress_reports_the_failure_and_nothing_is_saved_or_recorded()
    {
        ConfigureSuccessfulWallpaperVisit();
        fileClassificationRepository.IsAlreadyDownloadedAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);
        var exception = new InvalidOperationException("Navigating to 'https://wallhaven.cc/images/pic.jpg' did not produce a response.");
        imageDownloader.DownloadAsync(page, "https://wallhaven.cc/images/pic.jpg", Arg.Any<CancellationToken>()).Returns(Exceptional.Failure<byte[]>(exception));
        var sut = CreateSut();

        var result = await sut.ExecuteAsync(page, progress, TestContext.Current.CancellationToken);

        result.ShouldBeOfType<Success<FunctionalParadigm.Unit>>();
        progress.Received().Report(Arg.Is<string>(message => message!.Contains("Failed to download wallpaper image")));
        await fileStore.DidNotReceive().SaveAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<byte[]>(), Arg.Any<CancellationToken>());
        await fileClassificationRepository.DidNotReceive().RecordAsync(Arg.Any<IReadOnlyList<TagData>>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<long>(), Arg.Any<ImageDimensions>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_the_wallpaper_count_matches_the_stored_count_then_the_category_is_skipped_and_progress_is_reported()
    {
        contextReader.ReadAsync(Arg.Any<CancellationToken>()).Returns(singleCategoryContext);
        countReader.ReadAsync(page, Arg.Any<CancellationToken>()).Returns(42);
        var searchCategoryReader = Substitute.For<ISearchCategoryReader>();
        searchCategoryReader.GetLastKnownImageCountAsync("Nature", Arg.Any<CancellationToken>()).Returns(42);
        var sut = CreateSut(searchCategoryReader);

        var result = await sut.ExecuteAsync(page, progress, TestContext.Current.CancellationToken);

        result.ShouldBeOfType<Success<FunctionalParadigm.Unit>>();
        progress.Received().Report(Arg.Is<string>(message => message!.Contains("Category: Nature") && message!.Contains("already fully visited")));
        await page.DidNotReceive().GotoAsync(Arg.Is<string>(url => url!.Contains("&page=")), Arg.Any<PageGotoOptions>());
        await hrefCollector.DidNotReceive().CollectAsync(Arg.Any<IPage>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_the_wallpaper_count_differs_from_the_stored_count_then_the_category_is_scraped_normally()
    {
        contextReader.ReadAsync(Arg.Any<CancellationToken>()).Returns(singleCategoryContext);
        countReader.ReadAsync(page, Arg.Any<CancellationToken>()).Returns(24);
        var searchCategoryReader = Substitute.For<ISearchCategoryReader>();
        searchCategoryReader.GetLastKnownImageCountAsync("Nature", Arg.Any<CancellationToken>()).Returns(20);
        hrefCollector.CollectAsync(page, Arg.Any<CancellationToken>()).Returns([]);
        var sut = CreateSut(searchCategoryReader);

        var result = await sut.ExecuteAsync(page, progress, TestContext.Current.CancellationToken);

        result.ShouldBeOfType<Success<FunctionalParadigm.Unit>>();
        await page.Received(1).GotoAsync("https://wallhaven.cc/search?categories=1&page=1");
        await hrefCollector.Received(1).CollectAsync(page, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_reading_the_stored_image_count_returns_none_then_the_category_is_scraped_normally()
    {
        contextReader.ReadAsync(Arg.Any<CancellationToken>()).Returns(singleCategoryContext);
        countReader.ReadAsync(page, Arg.Any<CancellationToken>()).Returns(24);
        var searchCategoryReader = Substitute.For<ISearchCategoryReader>();
        searchCategoryReader.GetLastKnownImageCountAsync("Nature", Arg.Any<CancellationToken>()).Returns(Option<int>.None.Instance);
        hrefCollector.CollectAsync(page, Arg.Any<CancellationToken>()).Returns([]);
        var sut = CreateSut(searchCategoryReader);

        var result = await sut.ExecuteAsync(page, progress, TestContext.Current.CancellationToken);

        result.ShouldBeOfType<Success<FunctionalParadigm.Unit>>();
        await page.Received(1).GotoAsync("https://wallhaven.cc/search?categories=1&page=1");
        await hrefCollector.Received(1).CollectAsync(page, Arg.Any<CancellationToken>());
    }

    private void ConfigureSuccessfulWallpaperVisit()
    {
        contextReader.ReadAsync(Arg.Any<CancellationToken>()).Returns(singleCategoryContext);
        countReader.ReadAsync(page, Arg.Any<CancellationToken>()).Returns(1);
        hrefCollector.CollectAsync(page, Arg.Any<CancellationToken>()).Returns(["https://wallhaven.cc/w/abc123"]);
        var okResponse = Substitute.For<IResponse>();
        okResponse.Ok.Returns(true);
        page.GotoAsync("https://wallhaven.cc/w/abc123", Arg.Any<PageGotoOptions>()).Returns(okResponse);
        tagReader.ReadAsync(page, Arg.Any<CancellationToken>()).Returns([new TagData("Nature", "outdoors")]);
        imageLocator.LocateAsync(page, Arg.Any<CancellationToken>()).Returns(new Option<string>.Some("https://wallhaven.cc/images/pic.jpg"));
    }

    private SearchCategoryScrapeAction CreateSut(ISearchCategoryReader? searchCategoryReader = null)
    {
        if (searchCategoryReader == null)
        {
            searchCategoryReader = Substitute.For<ISearchCategoryReader>();
            searchCategoryReader.GetLastKnownImageCountAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(Option.None<int>());
        }

        return new(contextReader, categoryWriter, countReader, searchCategoryReader, hrefCollector, tagReader, imageLocator, imageDownloader, dimensionsReader, fileStore, categoryRegistrar, fileClassificationRepository);
    }
}
