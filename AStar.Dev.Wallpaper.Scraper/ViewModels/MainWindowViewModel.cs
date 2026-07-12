using AStar.Dev.Wallpaper.Scraper.Configuration;
using Microsoft.Extensions.Options;

namespace AStar.Dev.Wallpaper.Scraper.ViewModels;

public sealed class MainWindowViewModel : ViewModelBase
{
    public MainWindowViewModel(IOptions<ScrapeConfiguration> scrapeConfiguration)
    {
        var configuration = scrapeConfiguration.Value;

        Title = $"{configuration.ApplicationName} V{configuration.ApplicationVersion}";
    }

    public string Title { get; }
}
