using AStar.Dev.FunctionalParadigm;

namespace AStar.Dev.Wallpaper.Scraper.Scraping;

/// <summary>
///     Writes the SearchCategoryEntity to the database, updating as necessary.
/// </summary>
public interface ISearchCategoryWriter
{
    /// <summary>
    ///     Writes the current search categories, ignore lists, and directory layout to the database.
    /// </summary>
    /// <param name="searchCategory">The search category to write.</param>
    /// <param name="cancellationToken">A token used to observe cancellation of the write.</param>
    Task<Exceptional<Exception>> WriteAsync(SearchCategoryDto searchCategory, CancellationToken cancellationToken);
}
