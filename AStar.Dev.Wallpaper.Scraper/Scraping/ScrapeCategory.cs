namespace AStar.Dev.Wallpaper.Scraper.Scraping;

/// <summary>
///     Represents a single search category and the URL used to list its wallpapers.
/// </summary>
/// <param name="Name">The category's display name.</param>
/// <param name="SearchUrl">The fully-built search URL for this category, before any page number is appended.</param>
public sealed record ScrapeCategory(string Name, string SearchUrl);
