namespace AStar.Dev.Wallpaper.Scraper.Scraping;

/// <summary>
///     Persists a search category's scrape progress to the database.
/// </summary>
public interface ISearchCategoryWriter
{
    /// <summary>
    ///     Creates or updates the <c>SearchCategoryEntity</c> matching <paramref name="searchCategory" />'s name with its
    ///     famous/internet flags and latest scrape progress.
    /// </summary>
    /// <param name="searchCategory">The scrape progress to persist.</param>
    /// <param name="cancellationToken">A token used to observe cancellation of the write.</param>
    Task WriteAsync(SearchCategoryDto searchCategory, CancellationToken cancellationToken);
}
