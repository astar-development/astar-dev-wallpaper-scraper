using AStar.Dev.Wallpaper.Scraper.Scraping;
using Microsoft.Playwright;

namespace AStar.Dev.Wallpaper.Scraper.Tests.Unit.Scraping;

public sealed class GivenWallpaperImageDownloader
{
    [Fact]
    public async Task when_the_image_url_is_navigated_to_then_the_response_body_bytes_are_returned()
    {
        byte[] imageBytes = [1, 2, 3, 4];
        var page = Substitute.For<IPage>();
        var response = Substitute.For<IResponse>();
        response.BodyAsync().Returns(Task.FromResult(imageBytes));
        page.GotoAsync("https://wallhaven.cc/images/pic.jpg", Arg.Any<PageGotoOptions>()).Returns(Task.FromResult<IResponse?>(response));
        var sut = new WallpaperImageDownloader();

        var bytes = await sut.DownloadAsync(page, "https://wallhaven.cc/images/pic.jpg", TestContext.Current.CancellationToken);

        bytes.ShouldBe(imageBytes);
    }
}
