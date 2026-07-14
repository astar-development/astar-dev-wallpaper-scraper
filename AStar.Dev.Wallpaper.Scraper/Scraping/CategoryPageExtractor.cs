using Microsoft.Playwright;

namespace AStar.Dev.Wallpaper.Scraper.Scraping;

/// <summary>
///     Extracts search category names from an already-navigated Playwright page. The target site's DOM and
///     selectors are not yet known, so this currently returns no categories.
/// </summary>
public sealed class CategoryPageExtractor : ICategoryPageExtractor
{
    /// <inheritdoc />
    public Task<IReadOnlyList<string>> ExtractAsync(IPage page, CancellationToken token) =>
        Task.FromResult<IReadOnlyList<string>>([]);
}
