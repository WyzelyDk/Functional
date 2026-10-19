using System.Runtime.CompilerServices;

namespace Wyzely.Functional;

/// <summary>
/// Runs an operation that returns a result for every item of a sequence, with a bounded degree of parallelism, and combines
/// the results.
/// </summary>
/// <remarks>
/// <para>
/// These methods replace the array-of-results-filled-in-a-loop pattern, which is error prone (an unfilled slot is a
/// <see langword="default"/> result). They always attempt every item, so a failure does not hide the failures of the other
/// items; the errors of the failed items are aggregated in the order of the source, not the order in which they completed.
/// </para>
/// <para>
/// An exception thrown by the operation is not turned into a result (use <see cref="Result.Try(Func{Task}, Func{Exception, Error}?)"/>
/// inside the operation for that). Cancellation stops items that have not started yet and surfaces as an
/// <see cref="OperationCanceledException"/>.
/// </para>
/// </remarks>
public static class ResultParallelExtensions
{
    private const int Unbounded = -1;

    /// <summary>Runs an asynchronous operation that returns a result for every item and combines the values in source order.</summary>
    /// <typeparam name="TSource">The type of the items.</typeparam>
    /// <typeparam name="TResult">The type of the values of the results.</typeparam>
    /// <param name="source">The items to process. The sequence is enumerated once, before any operation starts.</param>
    /// <param name="operation">The operation to run for an item. It receives the cancellation token.</param>
    /// <param name="maxDegreeOfParallelism">
    /// The maximum number of operations that run at the same time, or <c>-1</c> for no limit. When omitted,
    /// <see cref="ResultOptions.DefaultMaxDegreeOfParallelism"/> is used.
    /// </param>
    /// <param name="cancellationToken">A token that stops items that have not started yet.</param>
    /// <returns>
    /// A task that produces a successful result with the values in the order of the source when every operation succeeded, or a
    /// failed result with the error (or the aggregate of the errors, in source order) of the failed items.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> or <paramref name="operation"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="maxDegreeOfParallelism"/> is zero or less than <c>-1</c>.</exception>
    [OverloadResolutionPriority(1)]
    public static Task<Result<IReadOnlyList<TResult>>> SelectInParallel<TSource, TResult>(
        this IEnumerable<TSource> source,
        Func<TSource, CancellationToken, Task<Result<TResult>>> operation,
        int? maxDegreeOfParallelism = null,
        CancellationToken cancellationToken = default)
    {
        Guard.NotNull(source, nameof(source));
        Guard.NotNull(operation, nameof(operation));
        var degree = ResolveDegree(maxDegreeOfParallelism);

        return SelectAsync(source, operation, degree, cancellationToken);
    }

    /// <summary>Runs an operation that returns a <see cref="ValueTask{TResult}"/> of a result for every item and combines the values in source order.</summary>
    /// <typeparam name="TSource">The type of the items.</typeparam>
    /// <typeparam name="TResult">The type of the values of the results.</typeparam>
    /// <param name="source">The items to process. The sequence is enumerated once, before any operation starts.</param>
    /// <param name="operation">The operation to run for an item. It receives the cancellation token.</param>
    /// <param name="maxDegreeOfParallelism">
    /// The maximum number of operations that run at the same time, or <c>-1</c> for no limit. When omitted,
    /// <see cref="ResultOptions.DefaultMaxDegreeOfParallelism"/> is used.
    /// </param>
    /// <param name="cancellationToken">A token that stops items that have not started yet.</param>
    /// <returns>
    /// See <see cref="SelectInParallel{TSource, TResult}(IEnumerable{TSource}, Func{TSource, CancellationToken, Task{Result{TResult}}}, int?,
    /// CancellationToken)"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> or <paramref name="operation"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="maxDegreeOfParallelism"/> is zero or less than <c>-1</c>.</exception>
    public static Task<Result<IReadOnlyList<TResult>>> SelectInParallel<TSource, TResult>(
        this IEnumerable<TSource> source,
        Func<TSource, CancellationToken, ValueTask<Result<TResult>>> operation,
        int? maxDegreeOfParallelism = null,
        CancellationToken cancellationToken = default)
    {
        Guard.NotNull(source, nameof(source));
        Guard.NotNull(operation, nameof(operation));
        var degree = ResolveDegree(maxDegreeOfParallelism);

        return SelectAsync(source, (item, token) => operation(item, token).AsTask(), degree, cancellationToken);
    }

    /// <summary>Runs an asynchronous operation that returns a result for every item and combines the results.</summary>
    /// <typeparam name="TSource">The type of the items.</typeparam>
    /// <param name="source">The items to process. The sequence is enumerated once, before any operation starts.</param>
    /// <param name="operation">The operation to run for an item. It receives the cancellation token.</param>
    /// <param name="maxDegreeOfParallelism">
    /// The maximum number of operations that run at the same time, or <c>-1</c> for no limit. When omitted,
    /// <see cref="ResultOptions.DefaultMaxDegreeOfParallelism"/> is used.
    /// </param>
    /// <param name="cancellationToken">A token that stops items that have not started yet.</param>
    /// <returns>
    /// A task that produces a successful result when every operation succeeded, or a failed result with the error (or the
    /// aggregate of the errors, in source order) of the failed items.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> or <paramref name="operation"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="maxDegreeOfParallelism"/> is zero or less than <c>-1</c>.</exception>
    [OverloadResolutionPriority(1)]
    public static Task<Result> ForEachInParallel<TSource>(
        this IEnumerable<TSource> source,
        Func<TSource, CancellationToken, Task<Result>> operation,
        int? maxDegreeOfParallelism = null,
        CancellationToken cancellationToken = default)
    {
        Guard.NotNull(source, nameof(source));
        Guard.NotNull(operation, nameof(operation));
        var degree = ResolveDegree(maxDegreeOfParallelism);

        return ForEachAsync(source, operation, degree, cancellationToken);
    }

    /// <summary>Runs an operation that returns a <see cref="ValueTask{TResult}"/> of a result for every item and combines the results.</summary>
    /// <typeparam name="TSource">The type of the items.</typeparam>
    /// <param name="source">The items to process. The sequence is enumerated once, before any operation starts.</param>
    /// <param name="operation">The operation to run for an item. It receives the cancellation token.</param>
    /// <param name="maxDegreeOfParallelism">
    /// The maximum number of operations that run at the same time, or <c>-1</c> for no limit. When omitted,
    /// <see cref="ResultOptions.DefaultMaxDegreeOfParallelism"/> is used.
    /// </param>
    /// <param name="cancellationToken">A token that stops items that have not started yet.</param>
    /// <returns>
    /// See
    /// <see cref="ForEachInParallel{TSource}(IEnumerable{TSource}, Func{TSource, CancellationToken, Task{Result}}, int?, CancellationToken)"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> or <paramref name="operation"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="maxDegreeOfParallelism"/> is zero or less than <c>-1</c>.</exception>
    public static Task<Result> ForEachInParallel<TSource>(
        this IEnumerable<TSource> source,
        Func<TSource, CancellationToken, ValueTask<Result>> operation,
        int? maxDegreeOfParallelism = null,
        CancellationToken cancellationToken = default)
    {
        Guard.NotNull(source, nameof(source));
        Guard.NotNull(operation, nameof(operation));
        var degree = ResolveDegree(maxDegreeOfParallelism);

        return ForEachAsync(source, (item, token) => operation(item, token).AsTask(), degree, cancellationToken);
    }

    /// <summary>Checks that a degree of parallelism is positive or <c>-1</c> (no limit).</summary>
    /// <param name="maxDegreeOfParallelism">The degree to check.</param>
    /// <param name="paramName">The name of the parameter that the exception reports.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="maxDegreeOfParallelism"/> is zero or less than <c>-1</c>.</exception>
    internal static void ValidateDegree(int maxDegreeOfParallelism, string paramName)
    {
        if (maxDegreeOfParallelism is 0 or < Unbounded)
        {
            throw new ArgumentOutOfRangeException(
                paramName,
                maxDegreeOfParallelism,
                "The maximum degree of parallelism must be positive, or -1 for no limit.");
        }
    }

    private static int ResolveDegree(int? maxDegreeOfParallelism)
    {
        if (maxDegreeOfParallelism is not { } degree)
        {
            return ResultConfiguration.Current.DefaultMaxDegreeOfParallelism;
        }

        ValidateDegree(degree, nameof(maxDegreeOfParallelism));

        return degree;
    }

    private static async Task<Result<IReadOnlyList<TResult>>> SelectAsync<TSource, TResult>(
        IEnumerable<TSource> source,
        Func<TSource, CancellationToken, Task<Result<TResult>>> operation,
        int maxDegreeOfParallelism,
        CancellationToken cancellationToken)
    {
        var items = source.ToList();
        var values = new TResult[items.Count];
        var errors = new Error?[items.Count];

        await RunAsync(
            items.Count,
            maxDegreeOfParallelism,
            async index =>
            {
                var outcome = await operation(items[index], cancellationToken).ConfigureAwait(ResultConfiguration.ContinueOnCapturedContext);

                var failure = outcome.Unpack(out var value);

                if (failure is null)
                {
                    values[index] = value;
                }
                else
                {
                    errors[index] = failure;
                }
            },
            cancellationToken).ConfigureAwait(ResultConfiguration.ContinueOnCapturedContext);

        var failures = errors.OfType<Error>().ToList();

        return failures.Count == 0
            ? Result.Success<IReadOnlyList<TResult>>(values)
            : Result.Failure<IReadOnlyList<TResult>>(Error.Aggregate(failures));
    }

    private static async Task<Result> ForEachAsync<TSource>(
        IEnumerable<TSource> source,
        Func<TSource, CancellationToken, Task<Result>> operation,
        int maxDegreeOfParallelism,
        CancellationToken cancellationToken)
    {
        var items = source.ToList();
        var errors = new Error?[items.Count];

        await RunAsync(
            items.Count,
            maxDegreeOfParallelism,
            async index =>
            {
                var outcome = await operation(items[index], cancellationToken).ConfigureAwait(ResultConfiguration.ContinueOnCapturedContext);

                if (outcome.TryGetError(out var error))
                {
                    errors[index] = error;
                }
            },
            cancellationToken).ConfigureAwait(ResultConfiguration.ContinueOnCapturedContext);

        var failures = errors.OfType<Error>().ToList();

        return failures.Count == 0 ? Result.Success() : Result.Failure(Error.Aggregate(failures));
    }

    private static async Task RunAsync(int count, int maxDegreeOfParallelism, Func<int, Task> body, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (count == 0)
        {
            return;
        }

        var limit = maxDegreeOfParallelism == Unbounded ? count : Math.Min(maxDegreeOfParallelism, count);

#if NET6_0_OR_GREATER
        // The limit is always explicit: Parallel.ForEachAsync treats the default of -1 as the number of processors, which would
        // silently throttle I/O-bound work that was asked to run without a limit.
        var options = new ParallelOptions { MaxDegreeOfParallelism = limit, CancellationToken = cancellationToken };

        await Parallel.ForEachAsync(Enumerable.Range(0, count), options, (index, _) => new ValueTask(body(index)))
            .ConfigureAwait(ResultConfiguration.ContinueOnCapturedContext);
#else
        using var gate = new SemaphoreSlim(limit);
        var tasks = new Task[count];

        for (var index = 0; index < count; index++)
        {
            tasks[index] = RunOneAsync(index);
        }

        await Task.WhenAll(tasks).ConfigureAwait(ResultConfiguration.ContinueOnCapturedContext);

        async Task RunOneAsync(int index)
        {
            await gate.WaitAsync(cancellationToken).ConfigureAwait(ResultConfiguration.ContinueOnCapturedContext);

            try
            {
                await body(index).ConfigureAwait(ResultConfiguration.ContinueOnCapturedContext);
            }
            finally
            {
                gate.Release();
            }
        }
#endif
    }
}
