namespace Wyzely.Functional;

/// <content>
/// <c>FirstFailureOrSuccess</c>: the first failure of a sequence of results, without looking at the rest.
/// </content>
public static partial class ResultCollectionExtensions
{
    /// <summary>
    /// Returns the first failed result of a sequence, or a successful result when none failed. The sequence is not enumerated further than the
    /// first failure, unlike <c>Combine</c>, which looks at every result and aggregates their errors.
    /// </summary>
    /// <param name="results">The results to look at. A lazy sequence runs only as far as the first failure.</param>
    /// <returns>The first failed result, or a successful result when every result succeeded (or the sequence is empty).</returns>
    /// <exception cref="ArgumentNullException"><paramref name="results"/> is <see langword="null"/>.</exception>
    public static Result FirstFailureOrSuccess(this IEnumerable<Result> results)
    {
        Guard.NotNull(results, nameof(results));

        foreach (var result in results)
        {
            if (result.IsFailure)
            {
                return result;
            }
        }

        return Result.Success();
    }

#if NET8_0_OR_GREATER
    /// <summary>
    /// Reads a stream of results up to the first failed one and returns it, or returns a successful result when none failed. The stream is not read
    /// further than the first failure.
    /// </summary>
    /// <param name="results">The stream of results to look at.</param>
    /// <param name="cancellationToken">A token that stops the reading of the stream.</param>
    /// <returns>A value task that produces the first failed result, or a successful result when every result succeeded.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="results"/> is <see langword="null"/>.</exception>
    public static ValueTask<Result> FirstFailureOrSuccess(this IAsyncEnumerable<Result> results, CancellationToken cancellationToken = default)
    {
        Guard.NotNull(results, nameof(results));

        return Core(results, cancellationToken);

        static async ValueTask<Result> Core(IAsyncEnumerable<Result> items, CancellationToken token)
        {
            await foreach (var result in items.WithCancellation(token).ConfigureAwait(ResultConfiguration.ContinueOnCapturedContext))
            {
                if (result.IsFailure)
                {
                    return result;
                }
            }

            return Result.Success();
        }
    }
#endif
}
