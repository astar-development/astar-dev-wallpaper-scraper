using AStar.Dev.FunctionalParadigm;

namespace AStar.Dev.Wallpaper.Scraper.Scraping;

/// <summary>
///     Reads search category data from the database.
/// </summary>
public interface ISearchCategoryReader
{
    /// <summary>
    ///     Retrieves the last known image count for a search category by name.
    /// </summary>
    /// <param name="categoryName">The name of the category to look up.</param>
    /// <param name="cancellationToken">A token used to observe cancellation of the read.</param>
    /// <returns>The stored image count if the category exists; <c>None</c> if not found.</returns>
    Task<Option<int>> GetLastKnownImageCountAsync(string categoryName, CancellationToken cancellationToken);
}
