using AStar.Dev.FunctionalParadigm;
using AStar.Dev.Infrastructure.AppDb;
using AStar.Dev.Infrastructure.AppDb.Entities;
using AStar.Dev.Wallpaper.Scraper.Scraping;
using Microsoft.EntityFrameworkCore;
using Microsoft.Playwright;

namespace AStar.Dev.Wallpaper.Scraper.Tests.Unit.Scraping;

public sealed class GivenSearchCategoryScrapeAction : IDisposable
{
    private readonly string databasePath = Path.Combine(Path.GetTempPath(), $"search-category-scrape-action-{Guid.NewGuid():N}.db");
    private readonly IDbContextFactory<AppDbContext> dbContextFactory;
    private readonly IProgress<string> progress = Substitute.For<IProgress<string>>();

    public GivenSearchCategoryScrapeAction()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite($"Data Source={databasePath}").Options;
        dbContextFactory = new TestDbContextFactory(options);

        using var context = dbContextFactory.CreateDbContext();
        context.Database.Migrate();
    }

    [Fact]
    public async Task when_a_category_has_more_wallpapers_than_fit_on_one_page_then_progress_reports_the_page_count_and_a_success_result_is_returned()
    {
        SeedSearchConfigurationWithCategory("Nature");
        var page = CreatePageReturningWallpaperCount(50);
        var sut = new SearchCategoryScrapeAction(dbContextFactory);

        var result = await sut.ExecuteAsync(page, progress, TestContext.Current.CancellationToken);

        result.ShouldBeOfType<Success<FunctionalParadigm.Unit>>();
        progress.Received().Report(Arg.Is<string>(message => message!.Contains("Visiting category: Nature")));
        progress.Received().Report(Arg.Is<string>(message => message!.Contains("need to get all 3 pages")));
    }

    [Fact]
    public async Task when_no_search_configuration_exists_then_a_failure_result_is_returned_instead_of_throwing()
    {
        var page = CreatePageReturningWallpaperCount(50);
        var sut = new SearchCategoryScrapeAction(dbContextFactory);

        var result = await sut.ExecuteAsync(page, progress, TestContext.Current.CancellationToken);

        result.ShouldBeOfType<Failure<FunctionalParadigm.Unit>>();
    }

    public void Dispose()
    {
        if (File.Exists(databasePath))
        {
            File.Delete(databasePath);
        }
    }

    private void SeedSearchConfigurationWithCategory(string categoryName)
    {
        using var context = dbContextFactory.CreateDbContext();
        var searchConfiguration = new SearchConfigurationEntity { SearchStringPrefix = "https://wallhaven.cc/search?categories=", SearchStringSuffix = "&sorting=random", };
        context.SearchConfigurations.Add(searchConfiguration);
        context.SaveChanges();

        context.SearchCategories.Add(new SearchCategoryEntity { Id = Guid.CreateVersion7().ToString(), SearchConfigurationId = searchConfiguration.Id, Name = categoryName, });
        context.SaveChanges();
    }

    private static IPage CreatePageReturningWallpaperCount(int wallpaperCount)
    {
        var page = Substitute.For<IPage>();
        var header = Substitute.For<ILocator>();
        header.AllTextContentsAsync().Returns(Task.FromResult<IReadOnlyList<string>>([$"{wallpaperCount} Wallpapers found for ..."]));
        page.GetByText(Arg.Any<string>(), Arg.Any<PageGetByTextOptions>()).Returns(header);

        return page;
    }

    private sealed class TestDbContextFactory(DbContextOptions<AppDbContext> options) : IDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext() => new(options);

        public Task<AppDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(new AppDbContext(options));
    }
}
