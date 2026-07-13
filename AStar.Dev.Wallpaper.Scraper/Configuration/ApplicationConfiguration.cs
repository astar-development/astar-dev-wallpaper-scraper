namespace AStar.Dev.Wallpaper.Scraper.Configuration;

public class ApplicationConfiguration
{
    public Serilog Serilog { get; set; } = new Serilog();
    public ScrapeConfiguration ScrapeConfiguration { get; set; } = new ScrapeConfiguration();
}
