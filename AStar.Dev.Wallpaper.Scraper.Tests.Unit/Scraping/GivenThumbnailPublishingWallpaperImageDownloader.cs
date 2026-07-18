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
        var sut = CreateSut(downloadResult: Exceptional.Success(imageBytes), thumbnailBytes: thumbnailBytes);

        var result = await sut.DownloadAsync(page, "https://wallhaven.cc/images/pic.jpg", TestContext.Current.CancellationToken);

        result.ShouldBeOfType<Success<byte[]>>();
        result.ShouldBe(new Success<byte[]>(imageBytes));
        thumbnailPublisher.Received(1).Publish(thumbnailBytes);
    }

    [Fact]
    public async Task when_the_inner_downloader_fails_then_no_thumbnail_is_generated_and_the_failure_is_returned()
    {
        var exception = new InvalidOperationException("Navigating to 'https://wallhaven.cc/images/pic.jpg' did not produce a response.");
        var sut = CreateSut(downloadResult: Exceptional.Failure<byte[]>(exception));

        var result = await sut.DownloadAsync(page, "https://wallhaven.cc/images/pic.jpg", TestContext.Current.CancellationToken);

        result.ShouldBeOfType<Failure<byte[]>>();
        result.ShouldBe(new Failure<byte[]>(exception));
        thumbnailGenerator.DidNotReceive().Generate(Arg.Any<byte[]>());
        thumbnailPublisher.DidNotReceive().Publish(Arg.Any<byte[]>());
    }

    private ThumbnailPublishingWallpaperImageDownloader CreateSut(Exceptional<byte[]> downloadResult, byte[]? thumbnailBytes = null)
    {
        inner.DownloadAsync(page, "https://wallhaven.cc/images/pic.jpg", TestContext.Current.CancellationToken).Returns(downloadResult);

        if (thumbnailBytes is not null && downloadResult is Success<byte[]> success)
        {
            thumbnailGenerator.Generate(success.Value).Returns(thumbnailBytes);
        }

        return new ThumbnailPublishingWallpaperImageDownloader(inner, thumbnailGenerator, thumbnailPublisher);
    }
}
