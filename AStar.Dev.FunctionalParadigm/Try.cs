namespace AStar.Dev.FunctionalParadigm;

/// <summary>
///     Bridges throwing operations into <see cref="Exceptional{T}" />, capturing thrown exceptions into a
///     <see cref="Failure{T}" /> instead of letting them propagate.
/// </summary>
/// <remarks>
///     <see cref="OperationCanceledException" /> (including <see cref="TaskCanceledException" />) is never
///     captured — it always rethrows, so <see cref="CancellationToken.ThrowIfCancellationRequested" /> semantics
///     survive.
/// </remarks>
public static class Try
{
    /// <summary>
    ///     Runs the specified operation, capturing any thrown exception (other than
    ///     <see cref="OperationCanceledException" />) into a <see cref="Failure{T}" />.
    /// </summary>
    public static Exceptional<T> Run<T>(Func<T> operation)
    {
        try
        {
            return new Success<T>(operation());
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            return new Failure<T>(exception);
        }
    }

    /// <summary>
    ///     Runs the specified operation, throwing immediately if <paramref name="cancellationToken" /> is
    ///     cancelled, and otherwise capturing any thrown exception (other than
    ///     <see cref="OperationCanceledException" />) into a <see cref="Failure{T}" />.
    /// </summary>
    public static Exceptional<T> Run<T>(Func<T> operation, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return Run(operation);
    }

    /// <summary>
    ///     Runs the specified asynchronous operation, capturing any thrown exception (other than
    ///     <see cref="OperationCanceledException" />) into a <see cref="Failure{T}" />.
    /// </summary>
    public static async Task<Exceptional<T>> RunAsync<T>(Func<Task<T>> operation)
    {
        try
        {
            var value = await operation().ConfigureAwait(false);

            return new Success<T>(value);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            return new Failure<T>(exception);
        }
    }

    /// <summary>
    ///     Runs the specified asynchronous operation, throwing immediately if <paramref name="cancellationToken" />
    ///     is cancelled, and otherwise capturing any thrown exception (other than
    ///     <see cref="OperationCanceledException" />) into a <see cref="Failure{T}" />.
    /// </summary>
    public static async Task<Exceptional<T>> RunAsync<T>(Func<Task<T>> operation, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await RunAsync(operation).ConfigureAwait(false);
    }

    public static Exceptional<T> Ensure<T>(this Exceptional<T> result, Action finallyAction)
    {
        finallyAction();
        return result;
    }
}
