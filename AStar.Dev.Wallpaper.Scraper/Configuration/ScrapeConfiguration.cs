namespace AStar.Dev.Wallpaper.Scraper.Configuration;

public class ScrapeConfiguration
{
    public string ApplicationName { get; set; } = string.Empty;

    public ConnectionStrings ConnectionStrings { get; set; } = new ConnectionStrings();

    /// <summary>
    ///     Gets or sets the directory used to persist the Playwright browser profile between sessions.
    /// </summary>
    public string UserDataDirectory { get; set; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "astar-dev-wallpaper-scraper", "playwright-profile");

    /// <summary>
    ///     Gets or sets the search parameters guiding the scraper's navigation of the target website.
    /// </summary>
    public SearchConfiguration SearchConfiguration { get; set; } = new SearchConfiguration();
}
