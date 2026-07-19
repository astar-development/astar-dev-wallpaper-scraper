namespace AStar.Dev.Wallpaper.Scraper.Configuration;

/// <summary>
///    Represents the size of a window in terms of width and height.
/// </summary>
/// <param name="Width">The width of the window.</param>
/// <param name="Height">The height of the window.</param>
public record WindowSize(double Width = 1_000, double Height = 1_000);