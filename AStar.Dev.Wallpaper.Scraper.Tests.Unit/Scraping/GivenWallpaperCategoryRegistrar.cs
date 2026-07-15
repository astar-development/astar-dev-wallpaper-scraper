using AStar.Dev.Infrastructure.AppDb;
using AStar.Dev.Infrastructure.AppDb.Entities;
using AStar.Dev.Wallpaper.Scraper.Scraping;
using Microsoft.EntityFrameworkCore;

namespace AStar.Dev.Wallpaper.Scraper.Tests.Unit.Scraping;

public sealed class GivenWallpaperCategoryRegistrar : IDisposable
{
    private readonly string databasePath = Path.Combine(Path.GetTempPath(), $"wallpaper-category-registrar-{Guid.NewGuid():N}.db");
    private readonly IDbContextFactory<AppDbContext> dbContextFactory;

    public GivenWallpaperCategoryRegistrar()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite($"Data Source={databasePath}").Options;
        dbContextFactory = new TestDbContextFactory(options);

        using var context = dbContextFactory.CreateDbContext();
        context.Database.Migrate();
    }

    [Fact]
    public async Task when_a_tagged_category_does_not_exist_then_it_is_added()
    {
        var sut = new WallpaperCategoryRegistrar(dbContextFactory);

        await sut.EnsureCategoriesExistAsync([new TagData("Nature", "outdoors")], TestContext.Current.CancellationToken);

        using var context = dbContextFactory.CreateDbContext();
        var category = context.FileClassificationCategories.Single(c => c.Name == "Nature");
        category.IsFamous.ShouldBeFalse();
        category.IsInternet.ShouldBeFalse();
        category.IncludeInSearch.ShouldBeTrue();
    }

    [Fact]
    public async Task when_a_tagged_category_already_exists_then_it_is_not_duplicated()
    {
        using (var context = dbContextFactory.CreateDbContext())
        {
            context.FileClassificationCategories.Add(new FileClassificationCategoryEntity { Name = "Nature", });
            context.SaveChanges();
        }

        var sut = new WallpaperCategoryRegistrar(dbContextFactory);
        await sut.EnsureCategoriesExistAsync([new TagData("Nature", "outdoors")], TestContext.Current.CancellationToken);

        using var verifyContext = dbContextFactory.CreateDbContext();
        verifyContext.FileClassificationCategories.Count(c => c.Name == "Nature").ShouldBe(1);
    }

    [Fact]
    public async Task when_a_tag_has_no_category_then_it_is_not_registered()
    {
        var sut = new WallpaperCategoryRegistrar(dbContextFactory);

        await sut.EnsureCategoriesExistAsync([new TagData("Untagged", null)], TestContext.Current.CancellationToken);

        using var context = dbContextFactory.CreateDbContext();
        context.FileClassificationCategories.Any(c => c.Name == "Untagged").ShouldBeFalse();
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
