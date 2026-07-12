using AStar.Dev.Wallpaper.Scraper.Configuration;
using AStar.Dev.Wallpaper.Scraper.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace AStar.Dev.Wallpaper.Scraper.Tests.Unit;

public class GivenAddApplicationServices
{
    private readonly ServiceProvider serviceProvider = new ServiceCollection()
        .AddApplicationServices()
        .BuildServiceProvider();

    [Fact]
    public void when_services_are_built_then_scrape_configuration_binds_from_appsettings()
    {
        var scrapeConfiguration = serviceProvider.GetRequiredService<IOptions<ScrapeConfiguration>>().Value;

        scrapeConfiguration.ApplicationName.ShouldBe("AStar Dev Wallpaper Scraper");
        scrapeConfiguration.ApplicationVersion.ShouldBe("0.1.0");
        scrapeConfiguration.ConnectionStrings.Sqlite.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public void when_main_window_view_model_is_resolved_then_title_comes_from_appsettings()
    {
        var viewModel = serviceProvider.GetRequiredService<MainWindowViewModel>();

        viewModel.Title.ShouldBe("AStar Dev Wallpaper Scraper V0.1.0");
    }
}
