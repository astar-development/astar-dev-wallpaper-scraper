using AStar.Dev.FunctionalParadigm;
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
        inner.DownloadAsync(page, "https://wallhaven.cc/images/pic.jpg", TestContext.Current.CancellationToken).Returns(Exceptional.Success(imageBytes));
        thumbnailGenerator.Generate(imageBytes).Returns(thumbnailBytes);
        var sut = new ThumbnailPublishingWallpaperImageDownloader(inner, thumbnailGenerator, thumbnailPublisher);

        var result = await sut.DownloadAsync(page, "https://wallhaven.cc/images/pic.jpg", TestContext.Current.CancellationToken);

        result.ShouldBeOfType<Success<byte[]>>();
        result.ShouldBe(new Success<byte[]>(imageBytes));
        thumbnailPublisher.Received(1).Publish(thumbnailBytes);
    }

    [Fact]
    public async Task when_the_inner_downloader_fails_then_no_thumbnail_is_generated_and_the_failure_is_returned()
    {
        var exception = new InvalidOperationException("Navigating to 'https://wallhaven.cc/images/pic.jpg' did not produce a response.");
        inner.DownloadAsync(page, "https://wallhaven.cc/images/pic.jpg", TestContext.Current.CancellationToken).Returns(Exceptional.Failure<byte[]>(exception));
        var sut = new ThumbnailPublishingWallpaperImageDownloader(inner, thumbnailGenerator, thumbnailPublisher);

        var result = await sut.DownloadAsync(page, "https://wallhaven.cc/images/pic.jpg", TestContext.Current.CancellationToken);

        result.ShouldBeOfType<Failure<byte[]>>();
        result.ShouldBe(new Failure<byte[]>(exception));
        thumbnailGenerator.DidNotReceive().Generate(Arg.Any<byte[]>());
        thumbnailPublisher.DidNotReceive().Publish(Arg.Any<byte[]>());
    }
}
