namespace AStar.Dev.Wallpaper.Scraper.Configuration;

public class ScrapeConfiguration
{
    public string ApplicationName { get; set; } = string.Empty;

    public string ApplicationVersion { get; set; } = string.Empty;

    public ConnectionStrings ConnectionStrings { get; set; } = new ConnectionStrings();
}
