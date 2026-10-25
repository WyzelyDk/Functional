#if NET8_0_OR_GREATER
using System.Runtime.CompilerServices;

namespace Wyzely.Functional;

/// <content>
/// The same helpers for an asynchronous stream of results: <c>Combine</c> and <c>Partition</c> read it, and <c>SelectEach</c> turns the result of a
/// collection into a stream of results. A stream is read once, in order, and the reading stops when the cancellation token is cancelled.
/// </content>
public static partial class ResultCollectionExtensions
{
    /// <summary>
    /// Reads a stream of results that carry no value and combines them into one result. See <see cref="Result.Combine(IEnumerable{Result})"/>.
    /// </summary>
    /// <param name="results">The stream of results to combine.</param>
    /// <param name="cancellationToken">A token that stops the reading of the stream.</param>
    /// <returns>
    /// A value task that produces a successful result, or a failed result with the error (or the aggregate of the errors) of the failed results.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="results"/> is <see langword="null"/>.</exception>
    public static ValueTask<Result> Combine(this IAsyncEnumerable<Result> results, CancellationToken cancellationToken = default)
    {
        Guard.NotNull(results, nameof(results));

        return CombineCore(results, cancellationToken);

        static async ValueTask<Result> CombineCore(IAsyncEnumerable<Result> items, CancellationToken token) =>
            Result.Combine(await ResultStreams.ReadAsync(items, token).ConfigureAwait(ResultConfiguration.ContinueOnCapturedContext));
    }

    /// <summary>
    /// Reads a stream of results that carry a value and combines them into one result with the list of the values. See
    /// <see cref="Result.Combine{T}(IEnumerable{Result{T}})"/>.
    /// </summary>
    /// <typeparam name="T">The type of the values.</typeparam>
    /// <param name="results">The stream of results to combine.</param>
    /// <param name="cancellationToken">A token that stops the reading of the stream.</param>
    /// <returns>A value task that produces the combined result, with the values in the order of the stream.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="results"/> is <see langword="null"/>.</exception>
    public static ValueTask<Result<IReadOnlyList<T>>> Combine<T>(this IAsyncEnumerable<Result<T>> results, CancellationToken cancellationToken = default)
    {
        Guard.NotNull(results, nameof(results));

        return CombineCore(results, cancellationToken);

        static async ValueTask<Result<IReadOnlyList<T>>> CombineCore(IAsyncEnumerable<Result<T>> items, CancellationToken token) =>
            Result.Combine(await ResultStreams.ReadAsync(items, token).ConfigureAwait(ResultConfiguration.ContinueOnCapturedContext));
    }

    /// <summary>
    /// Reads a stream of results and splits it into the values of the successful ones and the errors of the failed ones, both in the order of the
    /// stream.
    /// </summary>
    /// <typeparam name="T">The type of the values.</typeparam>
    /// <param name="results">The stream of results to split.</param>
    /// <param name="cancellationToken">A token that stops the reading of the stream.</param>
    /// <returns>A value task that produces the values and the errors. Unlike <c>Combine</c>, this keeps the successes when something failed.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="results"/> is <see langword="null"/>.</exception>
    public static ValueTask<(IReadOnlyList<T> Values, IReadOnlyList<Error> Errors)> Partition<T>(
        this IAsyncEnumerable<Result<T>> results,
        CancellationToken cancellationToken = default)
    {
        Guard.NotNull(results, nameof(results));

        return PartitionCore(results, cancellationToken);

        static async ValueTask<(IReadOnlyList<T> Values, IReadOnlyList<Error> Errors)> PartitionCore(
            IAsyncEnumerable<Result<T>> items,
            CancellationToken token)
        {
            var values = new List<T>();
            var errors = new List<Error>();

            await foreach (var result in items.WithCancellation(token).ConfigureAwait(ResultConfiguration.ContinueOnCapturedContext))
            {
                var failure = result.Unpack(out var value);

                if (failure is null)
                {
                    values.Add(value);
                }
                else
                {
                    errors.Add(failure);
                }
            }

            return (values, errors);
        }
    }

    /// <summary>
    /// Turns the result of a list into a stream of results: one for every item, as the selector produces it, and a single failure when the receiver
    /// failed. The selector runs for an item when the stream is read up to it, not before.
    /// </summary>
    /// <typeparam name="TSource">The type of the items.</typeparam>
    /// <typeparam name="TResult">The type of the values of the results that the selector returns.</typeparam>
    /// <param name="result">The result of the list.</param>
    /// <param name="selector">The operation to run for an item.</param>
    /// <returns>A stream of results. A failed item does not stop it.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="selector"/> is <see langword="null"/>.</exception>
    public static IAsyncEnumerable<Result<TResult>> SelectEach<TSource, TResult>(
        this Result<IReadOnlyList<TSource>> result,
        Func<TSource, Result<TResult>> selector)
    {
        Guard.NotNull(selector, nameof(selector));
        var failure = result.Unpack(out var items);

        return SelectEachCore(failure, items, item => new ValueTask<Result<TResult>>(selector(item)));
    }

    /// <summary>
    /// Turns the result of a list into a stream of results with an asynchronous selector. See
    /// <see cref="SelectEach{TSource, TResult}(Result{IReadOnlyList{TSource}}, Func{TSource, Result{TResult}})"/>.
    /// </summary>
    /// <typeparam name="TSource">The type of the items.</typeparam>
    /// <typeparam name="TResult">The type of the values of the results that the selector returns.</typeparam>
    /// <param name="result">The result of the list.</param>
    /// <param name="selector">The asynchronous operation to run for an item. It is awaited before the next item is read.</param>
    /// <returns>A stream of results. A failed item does not stop it.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="selector"/> is <see langword="null"/>.</exception>
    [OverloadResolutionPriority(1)]
    public static IAsyncEnumerable<Result<TResult>> SelectEach<TSource, TResult>(
        this Result<IReadOnlyList<TSource>> result,
        Func<TSource, Task<Result<TResult>>> selector)
    {
        Guard.NotNull(selector, nameof(selector));
        var failure = result.Unpack(out var items);

        return SelectEachCore(failure, items, item => new ValueTask<Result<TResult>>(selector(item)));
    }

    /// <summary>Turns the result of a list into a stream of results with a selector that returns a <see cref="ValueTask{TResult}"/>.</summary>
    /// <typeparam name="TSource">The type of the items.</typeparam>
    /// <typeparam name="TResult">The type of the values of the results that the selector returns.</typeparam>
    /// <param name="result">The result of the list.</param>
    /// <param name="selector">The asynchronous operation to run for an item. It is awaited before the next item is read.</param>
    /// <returns>A stream of results. A failed item does not stop it.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="selector"/> is <see langword="null"/>.</exception>
    public static IAsyncEnumerable<Result<TResult>> SelectEach<TSource, TResult>(
        this Result<IReadOnlyList<TSource>> result,
        Func<TSource, ValueTask<Result<TResult>>> selector)
    {
        Guard.NotNull(selector, nameof(selector));
        var failure = result.Unpack(out var items);

        return SelectEachCore(failure, items, selector);
    }

    /// <summary>Turns the result of a sequence into a stream of results: one for every item, and a single failure when the receiver failed.</summary>
    /// <typeparam name="TSource">The type of the items.</typeparam>
    /// <typeparam name="TResult">The type of the values of the results that the selector returns.</typeparam>
    /// <param name="result">The result of the sequence.</param>
    /// <param name="selector">The operation to run for an item.</param>
    /// <returns>A stream of results. A failed item does not stop it.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="selector"/> is <see langword="null"/>.</exception>
    public static IAsyncEnumerable<Result<TResult>> SelectEach<TSource, TResult>(
        this Result<IEnumerable<TSource>> result,
        Func<TSource, Result<TResult>> selector)
    {
        Guard.NotNull(selector, nameof(selector));
        var failure = result.Unpack(out var items);

        return SelectEachCore(failure, items, item => new ValueTask<Result<TResult>>(selector(item)));
    }

    /// <summary>Turns the result of a sequence into a stream of results with an asynchronous selector.</summary>
    /// <typeparam name="TSource">The type of the items.</typeparam>
    /// <typeparam name="TResult">The type of the values of the results that the selector returns.</typeparam>
    /// <param name="result">The result of the sequence.</param>
    /// <param name="selector">The asynchronous operation to run for an item. It is awaited before the next item is read.</param>
    /// <returns>A stream of results. A failed item does not stop it.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="selector"/> is <see langword="null"/>.</exception>
    [OverloadResolutionPriority(1)]
    public static IAsyncEnumerable<Result<TResult>> SelectEach<TSource, TResult>(
        this Result<IEnumerable<TSource>> result,
        Func<TSource, Task<Result<TResult>>> selector)
    {
        Guard.NotNull(selector, nameof(selector));
        var failure = result.Unpack(out var items);

        return SelectEachCore(failure, items, item => new ValueTask<Result<TResult>>(selector(item)));
    }

    /// <summary>Turns the result of a sequence into a stream of results with a selector that returns a <see cref="ValueTask{TResult}"/>.</summary>
    /// <typeparam name="TSource">The type of the items.</typeparam>
    /// <typeparam name="TResult">The type of the values of the results that the selector returns.</typeparam>
    /// <param name="result">The result of the sequence.</param>
    /// <param name="selector">The asynchronous operation to run for an item. It is awaited before the next item is read.</param>
    /// <returns>A stream of results. A failed item does not stop it.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="selector"/> is <see langword="null"/>.</exception>
    public static IAsyncEnumerable<Result<TResult>> SelectEach<TSource, TResult>(
        this Result<IEnumerable<TSource>> result,
        Func<TSource, ValueTask<Result<TResult>>> selector)
    {
        Guard.NotNull(selector, nameof(selector));
        var failure = result.Unpack(out var items);

        return SelectEachCore(failure, items, selector);
    }

    /// <summary>The stream behind <c>SelectEach</c>: one result for every item, or a single failure when the receiver failed.</summary>
    /// <typeparam name="TSource">The type of the items.</typeparam>
    /// <typeparam name="TResult">The type of the values of the results.</typeparam>
    /// <param name="failure">The error of the receiver, or <see langword="null"/> when it succeeded.</param>
    /// <param name="items">The items of a receiver that succeeded.</param>
    /// <param name="selector">The operation to run for an item.</param>
    /// <param name="cancellationToken">
    /// A token of the stream itself. <c>SelectEach</c> passes none; the token that a consumer gives to <c>WithCancellation</c> is combined with it.
    /// </param>
    /// <returns>The stream of results.</returns>
    internal static async IAsyncEnumerable<Result<TResult>> SelectEachCore<TSource, TResult>(
        Error? failure,
        IEnumerable<TSource>? items,
        Func<TSource, ValueTask<Result<TResult>>> selector,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (failure is not null)
        {
            yield return Result.Failure<TResult>(failure);

            yield break;
        }

        foreach (var item in items!)
        {
            cancellationToken.ThrowIfCancellationRequested();

            yield return await selector(item).ConfigureAwait(ResultConfiguration.ContinueOnCapturedContext);
        }
    }
}
#endif
