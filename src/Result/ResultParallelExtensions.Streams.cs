#if NET8_0_OR_GREATER
using System.Runtime.CompilerServices;

namespace Wyzely.Functional;

/// <content>
/// The same helpers for an asynchronous stream of items. The stream is read to the end first, once and in order, and then the operations run,
/// as they do for a sequence that is enumerated once before any operation starts: it does not feed work to the operations while it is still
/// being read.
/// </content>
public static partial class ResultParallelExtensions
{
    /// <summary>
    /// Reads a stream, runs an asynchronous operation that returns a result for every item, and combines the values in the order of the stream.
    /// </summary>
    /// <typeparam name="TSource">The type of the items.</typeparam>
    /// <typeparam name="TResult">The type of the values of the results.</typeparam>
    /// <param name="source">The stream of items to process. It is read to the end before any operation starts.</param>
    /// <param name="operation">The operation to run for an item. It receives the cancellation token.</param>
    /// <param name="maxDegreeOfParallelism">
    /// The maximum number of operations that run at the same time, or <c>-1</c> for no limit. When omitted,
    /// <see cref="ResultOptions.DefaultMaxDegreeOfParallelism"/> is used.
    /// </param>
    /// <param name="cancellationToken">A token that stops the reading of the stream and the items that have not started yet.</param>
    /// <returns>
    /// A task that produces a successful result with the values in the order of the stream, or a failed result with the error (or the aggregate
    /// of the errors, in the order of the stream) of the failed items.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> or <paramref name="operation"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="maxDegreeOfParallelism"/> is zero or less than <c>-1</c>.</exception>
    [OverloadResolutionPriority(1)]
    public static Task<Result<IReadOnlyList<TResult>>> SelectInParallel<TSource, TResult>(
        this IAsyncEnumerable<TSource> source,
        Func<TSource, CancellationToken, Task<Result<TResult>>> operation,
        int? maxDegreeOfParallelism = null,
        CancellationToken cancellationToken = default)
    {
        Guard.NotNull(source, nameof(source));
        Guard.NotNull(operation, nameof(operation));
        var degree = ResolveDegree(maxDegreeOfParallelism);

        return SelectFromStreamAsync(source, operation, degree, cancellationToken);
    }

    /// <summary>Reads a stream and runs an operation that returns a <see cref="ValueTask{TResult}"/> of a result for every item.</summary>
    /// <typeparam name="TSource">The type of the items.</typeparam>
    /// <typeparam name="TResult">The type of the values of the results.</typeparam>
    /// <param name="source">The stream of items to process. It is read to the end before any operation starts.</param>
    /// <param name="operation">The operation to run for an item. It receives the cancellation token.</param>
    /// <param name="maxDegreeOfParallelism">
    /// The maximum number of operations that run at the same time, or <c>-1</c> for no limit. When omitted,
    /// <see cref="ResultOptions.DefaultMaxDegreeOfParallelism"/> is used.
    /// </param>
    /// <param name="cancellationToken">A token that stops the reading of the stream and the items that have not started yet.</param>
    /// <returns>A task that produces the combined result, as for an operation that returns a <see cref="Task{TResult}"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> or <paramref name="operation"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="maxDegreeOfParallelism"/> is zero or less than <c>-1</c>.</exception>
    public static Task<Result<IReadOnlyList<TResult>>> SelectInParallel<TSource, TResult>(
        this IAsyncEnumerable<TSource> source,
        Func<TSource, CancellationToken, ValueTask<Result<TResult>>> operation,
        int? maxDegreeOfParallelism = null,
        CancellationToken cancellationToken = default)
    {
        Guard.NotNull(source, nameof(source));
        Guard.NotNull(operation, nameof(operation));
        var degree = ResolveDegree(maxDegreeOfParallelism);

        return SelectFromStreamAsync(source, (item, token) => operation(item, token).AsTask(), degree, cancellationToken);
    }

    /// <summary>Reads a stream, runs an asynchronous operation that returns a result for every item, and combines the results.</summary>
    /// <typeparam name="TSource">The type of the items.</typeparam>
    /// <param name="source">The stream of items to process. It is read to the end before any operation starts.</param>
    /// <param name="operation">The operation to run for an item. It receives the cancellation token.</param>
    /// <param name="maxDegreeOfParallelism">
    /// The maximum number of operations that run at the same time, or <c>-1</c> for no limit. When omitted,
    /// <see cref="ResultOptions.DefaultMaxDegreeOfParallelism"/> is used.
    /// </param>
    /// <param name="cancellationToken">A token that stops the reading of the stream and the items that have not started yet.</param>
    /// <returns>
    /// A task that produces a successful result when every operation succeeded, or a failed result with the error (or the aggregate of the
    /// errors, in the order of the stream) of the failed items.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> or <paramref name="operation"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="maxDegreeOfParallelism"/> is zero or less than <c>-1</c>.</exception>
    [OverloadResolutionPriority(1)]
    public static Task<Result> ForEachInParallel<TSource>(
        this IAsyncEnumerable<TSource> source,
        Func<TSource, CancellationToken, Task<Result>> operation,
        int? maxDegreeOfParallelism = null,
        CancellationToken cancellationToken = default)
    {
        Guard.NotNull(source, nameof(source));
        Guard.NotNull(operation, nameof(operation));
        var degree = ResolveDegree(maxDegreeOfParallelism);

        return ForEachFromStreamAsync(source, operation, degree, cancellationToken);
    }

    /// <summary>Reads a stream and runs an operation that returns a <see cref="ValueTask{TResult}"/> of a result for every item.</summary>
    /// <typeparam name="TSource">The type of the items.</typeparam>
    /// <param name="source">The stream of items to process. It is read to the end before any operation starts.</param>
    /// <param name="operation">The operation to run for an item. It receives the cancellation token.</param>
    /// <param name="maxDegreeOfParallelism">
    /// The maximum number of operations that run at the same time, or <c>-1</c> for no limit. When omitted,
    /// <see cref="ResultOptions.DefaultMaxDegreeOfParallelism"/> is used.
    /// </param>
    /// <param name="cancellationToken">A token that stops the reading of the stream and the items that have not started yet.</param>
    /// <returns>A task that produces the combined result, as for an operation that returns a <see cref="Task{TResult}"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> or <paramref name="operation"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="maxDegreeOfParallelism"/> is zero or less than <c>-1</c>.</exception>
    public static Task<Result> ForEachInParallel<TSource>(
        this IAsyncEnumerable<TSource> source,
        Func<TSource, CancellationToken, ValueTask<Result>> operation,
        int? maxDegreeOfParallelism = null,
        CancellationToken cancellationToken = default)
    {
        Guard.NotNull(source, nameof(source));
        Guard.NotNull(operation, nameof(operation));
        var degree = ResolveDegree(maxDegreeOfParallelism);

        return ForEachFromStreamAsync(source, (item, token) => operation(item, token).AsTask(), degree, cancellationToken);
    }

    private static async Task<Result<IReadOnlyList<TResult>>> SelectFromStreamAsync<TSource, TResult>(
        IAsyncEnumerable<TSource> source,
        Func<TSource, CancellationToken, Task<Result<TResult>>> operation,
        int maxDegreeOfParallelism,
        CancellationToken cancellationToken)
    {
        var items = await ResultStreams.ReadAsync(source, cancellationToken).ConfigureAwait(ResultConfiguration.ContinueOnCapturedContext);

        return await SelectAsync(items, operation, maxDegreeOfParallelism, cancellationToken).ConfigureAwait(ResultConfiguration.ContinueOnCapturedContext);
    }

    private static async Task<Result> ForEachFromStreamAsync<TSource>(
        IAsyncEnumerable<TSource> source,
        Func<TSource, CancellationToken, Task<Result>> operation,
        int maxDegreeOfParallelism,
        CancellationToken cancellationToken)
    {
        var items = await ResultStreams.ReadAsync(source, cancellationToken).ConfigureAwait(ResultConfiguration.ContinueOnCapturedContext);

        return await ForEachAsync(items, operation, maxDegreeOfParallelism, cancellationToken).ConfigureAwait(ResultConfiguration.ContinueOnCapturedContext);
    }
}
#endif
