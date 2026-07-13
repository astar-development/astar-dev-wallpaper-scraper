using AStar.Dev.Wallpaper.Scraper.Services;
using Microsoft.Extensions.DependencyInjection;

namespace AStar.Dev.Wallpaper.Scraper.Tests.Unit;

public class GivenServiceCollectionExtensions
{
    [Fact]
    public void when_application_services_are_added_then_the_playwright_service_is_registered_as_a_singleton()
    {
        var services = new ServiceCollection().AddApplicationServices();

        var descriptor = services.Single(service => service.ServiceType == typeof(IPlaywrightService));
        descriptor.Lifetime.ShouldBe(ServiceLifetime.Singleton);
        descriptor.ImplementationType.ShouldBe(typeof(PlaywrightService));
    }
}
