namespace AStar.Dev.Wallpaper.Scraper.Configuration;

public record ClientConfiguration
{
    internal static string SectionName => "AStarDevOneDriveClient";

    public required string ApplicationName { get; init; }
    public required string ApplicationVersion { get; init; }
}
