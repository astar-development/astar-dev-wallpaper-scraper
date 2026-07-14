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
    private readonly ICategoryPageExtractor categoryPageExtractor = Substitute.For<ICategoryPageExtractor>();
    private readonly IDbContextFactory<AppDbContext> dbContextFactory;

    public GivenSearchCategoryScrapeAction()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite($"Data Source={databasePath}").Options;
        dbContextFactory = new TestDbContextFactory(options);

        using var context = dbContextFactory.CreateDbContext();
        context.Database.Migrate();
    }

    [Fact]
    public async Task when_extraction_succeeds_then_categories_are_persisted_beneath_unclassified_and_a_success_result_is_returned()
    {
        categoryPageExtractor.ExtractAsync(Arg.Any<IPage>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<string>>(["Nature"]));
        var sut = new SearchCategoryScrapeAction(categoryPageExtractor, dbContextFactory);

        var result = await sut.ExecuteAsync(Substitute.For<IPage>(), TestContext.Current.CancellationToken);

        result.ShouldBeOfType<Success<FunctionalParadigm.Unit>>();
        using var context = dbContextFactory.CreateDbContext();
        var root = await context.Set<FileClassificationCategoryEntity>()
            .SingleAsync(category => category.Level == 1 && category.Name == "Unclassified", TestContext.Current.CancellationToken);
        var category = await context.Set<FileClassificationCategoryEntity>()
            .SingleAsync(entity => entity.Name == "Nature", TestContext.Current.CancellationToken);
        category.ParentId.ShouldBe(root.Id);
    }

    [Fact]
    public async Task when_extraction_throws_then_a_failure_result_is_returned_instead_of_throwing()
    {
        categoryPageExtractor.ExtractAsync(Arg.Any<IPage>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<IReadOnlyList<string>>(new InvalidOperationException("boom")));
        var sut = new SearchCategoryScrapeAction(categoryPageExtractor, dbContextFactory);

        var result = await sut.ExecuteAsync(Substitute.For<IPage>(), TestContext.Current.CancellationToken);

        var failure = result.ShouldBeOfType<Failure<FunctionalParadigm.Unit>>();
        failure.Exception.Message.ShouldBe("boom");
    }

    public void Dispose()
    {
        if (File.Exists(databasePath))
        {
            File.Delete(databasePath);
        }
    }

    private sealed class TestDbContextFactory(DbContextOptions<AppDbContext> options) : IDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext() => new(options);

        public Task<AppDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(new AppDbContext(options));
    }
}
