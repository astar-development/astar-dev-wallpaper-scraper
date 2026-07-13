namespace AStar.Dev.Wallpaper.Scraper.Tests.Unit.Services;

public sealed class GivenPlaywrightService
{
    [Fact]
    public async Task WhenConstructed_ThenItIsNotNull()
    {
        var sut = CreatePlaywrightService();

        var result = await sut.ConfigurePlaywrightAsync();

        result.ShouldBeAssignableTo<Microsoft.Playwright.IPlaywright>();
    }

    private IPlaywrightService CreatePlaywrightService()
    {
        throw new NotImplementedException();
    }
}