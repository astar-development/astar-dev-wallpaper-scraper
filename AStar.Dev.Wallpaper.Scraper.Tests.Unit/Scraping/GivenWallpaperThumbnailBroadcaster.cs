using AStar.Dev.Wallpaper.Scraper.Scraping;

namespace AStar.Dev.Wallpaper.Scraper.Tests.Unit.Scraping;

public sealed class GivenWallpaperThumbnailBroadcaster : IDisposable
{
    private readonly WallpaperThumbnailBroadcaster sut = new();

    public void Dispose() => sut.Dispose();

    [Fact]
    public void when_a_thumbnail_is_published_then_subscribers_to_the_feed_receive_it()
    {
        byte[] thumbnailBytes = [1, 2, 3];
        byte[]? received = null;
        using var subscription = sut.Thumbnails.Subscribe(bytes => received = bytes);

        sut.Publish(thumbnailBytes);

        received.ShouldBe(thumbnailBytes);
    }

    [Fact]
    public void when_no_subscriber_is_present_then_publishing_does_not_throw()
    {
        byte[] thumbnailBytes = [1, 2, 3];

        var exception = Record.Exception(() => sut.Publish(thumbnailBytes));

        exception.ShouldBeNull();
    }
}
