namespace AStar.Dev.Wallpaper.Scraper.Configuration;

public class ApplicationConfiguration
{
    public Logging Logging { get; set; } = new Logging();

    public ScrapeConfiguration ScrapeConfiguration { get; set; } = new ScrapeConfiguration();
}