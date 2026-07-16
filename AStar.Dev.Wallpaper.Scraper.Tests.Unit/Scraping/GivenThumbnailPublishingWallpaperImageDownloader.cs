using AStar.Dev.Wallpaper.Scraper.Scraping;
using Microsoft.Playwright;

namespace AStar.Dev.Wallpaper.Scraper.Tests.Unit.Scraping;

public sealed class GivenThumbnailPublishingWallpaperImageDownloader
{
    private readonly IWallpaperImageDownloader inner = Substitute.For<IWallpaperImageDownloader>();
    private readonly IWallpaperThumbnailGenerator thumbnailGenerator = Substitute.For<IWallpaperThumbnailGenerator>();
    private readonly IWallpaperThumbnailPublisher thumbnailPublisher = Substitute.For<IWallpaperThumbnailPublisher>();
    private readonly IPage page = Substitute.For<IPage>();

    [Fact]
    public async Task when_the_inner_downloader_returns_bytes_then_a_thumbnail_is_generated_and_published()
    {
        byte[] imageBytes = [1, 2, 3];
        byte[] thumbnailBytes = [4, 5, 6];
        inner.DownloadAsync(page, "https://wallhaven.cc/images/pic.jpg", TestContext.Current.CancellationToken).Returns(Task.FromResult(imageBytes));
        thumbnailGenerator.Generate(imageBytes).Returns(thumbnailBytes);
        var sut = new ThumbnailPublishingWallpaperImageDownloader(inner, thumbnailGenerator, thumbnailPublisher);

        var result = await sut.DownloadAsync(page, "https://wallhaven.cc/images/pic.jpg", TestContext.Current.CancellationToken);

        result.ShouldBe(imageBytes);
        thumbnailPublisher.Received(1).Publish(thumbnailBytes);
    }
}
