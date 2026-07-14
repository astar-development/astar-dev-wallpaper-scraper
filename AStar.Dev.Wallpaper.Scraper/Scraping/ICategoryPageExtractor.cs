using Microsoft.Playwright;

namespace AStar.Dev.Wallpaper.Scraper.Scraping;

/// <summary>
///     Extracts search category names from an already-navigated Playwright page.
/// </summary>
public interface ICategoryPageExtractor
{
    /// <summary>
    ///     Reads the category names present on the current page.
    /// </summary>
    /// <param name="page">The Playwright page, already navigated to the category listing.</param>
    /// <param name="token">A token used to observe cancellation of the extraction.</param>
    Task<IReadOnlyList<string>> ExtractAsync(IPage page, CancellationToken token);
}
