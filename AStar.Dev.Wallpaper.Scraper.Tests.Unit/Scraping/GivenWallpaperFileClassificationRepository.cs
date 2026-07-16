using AStar.Dev.Infrastructure.AppDb;
using AStar.Dev.Infrastructure.AppDb.Entities;
using AStar.Dev.Wallpaper.Scraper.Scraping;
using Microsoft.EntityFrameworkCore;

namespace AStar.Dev.Wallpaper.Scraper.Tests.Unit.Scraping;

public sealed class GivenWallpaperFileClassificationRepository : IDisposable
{
    private readonly string databasePath = Path.Combine(Path.GetTempPath(), $"wallpaper-file-classification-repository-{Guid.NewGuid():N}.db");
    private readonly IDbContextFactory<AppDbContext> dbContextFactory;

    public GivenWallpaperFileClassificationRepository()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite($"Data Source={databasePath}").Options;
        dbContextFactory = new TestDbContextFactory(options);

        using var context = dbContextFactory.CreateDbContext();
        context.Database.Migrate();
    }

    [Fact]
    public async Task when_no_file_exists_at_the_directory_and_name_then_is_already_downloaded_is_false()
    {
        var sut = new WallpaperFileClassificationRepository(dbContextFactory);

        var alreadyDownloaded = await sut.IsAlreadyDownloadedAsync("/wallpapers/nature", "pic.jpg", TestContext.Current.CancellationToken);

        alreadyDownloaded.ShouldBeFalse();
    }

    [Fact]
    public async Task when_a_file_already_exists_at_the_directory_and_name_then_is_already_downloaded_is_true()
    {
        using (var context = dbContextFactory.CreateDbContext())
        {
            context.Files.Add(new FileDetailEntity
            {
                FileName = new FileName("pic.jpg"),
                DirectoryName = new DirectoryName("/wallpapers/nature"),
                FileHandle = new FileHandle(Guid.NewGuid().ToString()),
            });
            context.SaveChanges();
        }

        var sut = new WallpaperFileClassificationRepository(dbContextFactory);

        var alreadyDownloaded = await sut.IsAlreadyDownloadedAsync("/wallpapers/nature", "pic.jpg", TestContext.Current.CancellationToken);

        alreadyDownloaded.ShouldBeTrue();
    }

    [Fact]
    public async Task when_a_wallpaper_with_multiple_tags_is_recorded_then_a_file_classification_is_created_per_tag()
    {
        using (var context = dbContextFactory.CreateDbContext())
        {
            context.FileClassificationCategories.Add(new FileClassificationCategoryEntity { Name = "Nature", });
            context.FileClassificationCategories.Add(new FileClassificationCategoryEntity { Name = "Outdoors", });
            context.SaveChanges();
        }

        var sut = new WallpaperFileClassificationRepository(dbContextFactory);
        List<TagData> tags = [new("Nature", "outdoors"), new("Outdoors", "outdoors")];

        await sut.RecordAsync(tags, "https://wallhaven.cc/images/pic.jpg", "/wallpapers/nature", 1234, new ImageDimensions(100, 200), TestContext.Current.CancellationToken);

        using var verifyContext = dbContextFactory.CreateDbContext();
        var classifications = verifyContext.FileClassifications.Include(c => c.FileDetail).ThenInclude(d => d!.ImageDetail).Include(c => c.Category).ToList();
        classifications.Count.ShouldBe(2);
        classifications.ShouldAllBe(c => c.FileDetail!.FileName.Value == "pic.jpg");
        classifications.ShouldAllBe(c => c.FileDetail!.DirectoryName.Value == "/wallpapers/nature");
        classifications.ShouldAllBe(c => c.FileDetail!.FileSize == 1234);
        classifications.ShouldAllBe(c => c.FileDetail!.ImageDetail.Width == 100 && c.FileDetail!.ImageDetail.Height == 200);
        classifications.Select(c => c.Category!.Name).ShouldBe(["Nature", "Outdoors"], ignoreOrder: true);
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
