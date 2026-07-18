using AStar.Dev.Infrastructure.AppDb;
using AStar.Dev.Wallpaper.Scraper.Scraping;
using AStar.Dev.Wallpaper.Scraper.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AStar.Dev.Wallpaper.Scraper.Tests.Unit;

public class GivenServiceCollectionExtensions
{
    [Fact]
    public void when_application_services_are_added_then_the_playwright_service_is_registered_as_a_singleton()
    {
        var services = CreateServices();

        var descriptor = services.Single(service => service.ServiceType == typeof(IPlaywrightService));
        descriptor.Lifetime.ShouldBe(ServiceLifetime.Singleton);
        descriptor.ImplementationType.ShouldBe(typeof(PlaywrightService));
    }

    [Fact]
    public void when_application_services_are_added_then_the_app_db_context_factory_is_registered()
    {
        var services = CreateServices();

        services.ShouldContain(service => service.ServiceType == typeof(IDbContextFactory<AppDbContext>));
    }

    [Fact]
    public void when_application_services_are_added_then_the_search_category_scrape_action_is_registered_as_the_scrape_action()
    {
        var services = CreateServices();

        var descriptor = services.Single(service => service.ServiceType == typeof(IScrapeAction));
        descriptor.Lifetime.ShouldBe(ServiceLifetime.Singleton);
        descriptor.ImplementationType.ShouldBe(typeof(SearchCategoryScrapeAction));
    }

    private static IServiceCollection CreateServices()
    {
        IConfiguration configuration = new ConfigurationBuilder().Build();

        return new ServiceCollection().AddApplicationServices(configuration);
    }
}
