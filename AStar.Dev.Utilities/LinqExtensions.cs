using AStar.Dev.FunctionalParadigm;

namespace AStar.Dev.Utilities;

/// <summary>
/// </summary>
public static class LinqExtensions
{
    /// <summary>
    /// </summary>
    /// <param name="enumerable"></param>
    /// <param name="action"></param>
    /// <typeparam name="T"></typeparam>
    public static void ForEach<T>(this IEnumerable<T> enumerable, Action<T> action)
    {
        if (enumerable == null || action == null) return;

        foreach(var item in enumerable) action(item);
    }

    /// <summary>
    ///     Awaits the supplied asynchronous action once per item, strictly sequentially, so no two invocations
    ///     ever run concurrently.
    /// </summary>
    /// <param name="enumerable"></param>
    /// <param name="action"></param>
    /// <typeparam name="T"></typeparam>
    public static async Task ForEachAsync<T>(this IEnumerable<T> enumerable, Func<T, Task> action)
    {
        foreach (var item in enumerable)
            await action(item).ConfigureAwait(false);
    }

    /// <summary>
    ///     Returns the first element of <paramref name="source" /> as an <see cref="Option{T}" />, or <c>None</c> if the
    ///     sequence is empty.
    /// </summary>
    /// <param name="source">The sequence to search.</param>
    /// <typeparam name="T">The element type.</typeparam>
    public static Option<T> FirstOrNone<T>(this IEnumerable<T> source)
    {
        foreach (var item in source)
            return new Option<T>.Some(item);

        return Option.None<T>();
    }

    /// <summary>
    ///     Returns the first element of <paramref name="source" /> matching <paramref name="predicate" /> as an
    ///     <see cref="Option{T}" />, or <c>None</c> if no element matches.
    /// </summary>
    /// <param name="source">The sequence to search.</param>
    /// <param name="predicate">The condition an element must satisfy.</param>
    /// <typeparam name="T">The element type.</typeparam>
    public static Option<T> FirstOrNone<T>(this IEnumerable<T> source, Func<T, bool> predicate)
    {
        foreach (var item in source)
            if (predicate(item))
                return new Option<T>.Some(item);

        return Option.None<T>();
    }

    /// <summary>
    ///     Asynchronously returns the first element of <paramref name="source" /> as an <see cref="Option{T}" />, or
    ///     <c>None</c> if the sequence is empty.
    /// </summary>
    /// <param name="source">The sequence to search.</param>
    /// <param name="cancellationToken">A token used to observe cancellation of the enumeration.</param>
    /// <typeparam name="T">The element type.</typeparam>
    public static async Task<Option<T>> FirstOrNoneAsync<T>(this IAsyncEnumerable<T> source, CancellationToken cancellationToken = default)
    {
        await foreach (var item in source.WithCancellation(cancellationToken))
            return new Option<T>.Some(item);

        return Option.None<T>();
    }

    /// <summary>
    ///     Asynchronously returns the first element of <paramref name="source" /> matching <paramref name="predicate" />
    ///     as an <see cref="Option{T}" />, or <c>None</c> if no element matches.
    /// </summary>
    /// <param name="source">The sequence to search.</param>
    /// <param name="predicate">The condition an element must satisfy.</param>
    /// <param name="cancellationToken">A token used to observe cancellation of the enumeration.</param>
    /// <typeparam name="T">The element type.</typeparam>
    public static async Task<Option<T>> FirstOrNoneAsync<T>(this IAsyncEnumerable<T> source, Func<T, bool> predicate, CancellationToken cancellationToken = default)
    {
        await foreach (var item in source.WithCancellation(cancellationToken))
            if (predicate(item))
                return new Option<T>.Some(item);

        return Option.None<T>();
    }
}
