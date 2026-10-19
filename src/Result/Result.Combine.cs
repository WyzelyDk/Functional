namespace Wyzely.Functional;

/// <content>
/// <c>Combine</c>: turns many results into one. Every result is looked at (there is no short-circuit), so a combined failure
/// carries the errors of all failed results, in the order of the sequence.
/// </content>
public readonly partial struct Result
{
    /// <summary>Combines results that carry no value into one result.</summary>
    /// <param name="results">The results to combine.</param>
    /// <returns>
    /// A successful result when every result succeeded (or the sequence is empty). Otherwise a failed result whose error is the
    /// only error, or an aggregate of all errors in sequence order (see <see cref="Error.Aggregate(IEnumerable{Error})"/>).
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="results"/> is <see langword="null"/>.</exception>
    public static Result Combine(IEnumerable<Result> results) => CombineCore(Guard.NotNull(results, nameof(results)));

    /// <summary>Combines results that carry a value into one result that carries the list of the values.</summary>
    /// <typeparam name="T">The type of the values.</typeparam>
    /// <param name="results">The results to combine.</param>
    /// <returns>
    /// A successful result that holds the values in sequence order when every result succeeded (an empty list for an empty
    /// sequence). Otherwise a failed result whose error is the only error, or an aggregate of all errors in sequence order.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="results"/> is <see langword="null"/>.</exception>
    public static Result<IReadOnlyList<T>> Combine<T>(IEnumerable<Result<T>> results) => CombineCore(Guard.NotNull(results, nameof(results)));

    /// <summary>Waits for all tasks and combines their results, which carry no value, into one result.</summary>
    /// <param name="results">The tasks that produce the results. They should already be running.</param>
    /// <returns>A task that produces the combined result. See <see cref="Combine(IEnumerable{Result})"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="results"/> is <see langword="null"/>.</exception>
    /// <remarks>An exception thrown by a task is not turned into a result: it escapes from the returned task.</remarks>
    public static Task<Result> Combine(IEnumerable<Task<Result>> results)
    {
        Guard.NotNull(results, nameof(results));

        return Core(results);

        static async Task<Result> Core(IEnumerable<Task<Result>> results) =>
            CombineCore(await Task.WhenAll(results).ConfigureAwait(ResultConfiguration.ContinueOnCapturedContext));
    }

    /// <summary>Waits for all tasks and combines their results, which carry a value, into one result that carries the list of the values.</summary>
    /// <typeparam name="T">The type of the values.</typeparam>
    /// <param name="results">The tasks that produce the results. They should already be running.</param>
    /// <returns>A task that produces the combined result. See <see cref="Combine{T}(IEnumerable{Result{T}})"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="results"/> is <see langword="null"/>.</exception>
    /// <remarks>An exception thrown by a task is not turned into a result: it escapes from the returned task.</remarks>
    public static Task<Result<IReadOnlyList<T>>> Combine<T>(IEnumerable<Task<Result<T>>> results)
    {
        Guard.NotNull(results, nameof(results));

        return Core(results);

        static async Task<Result<IReadOnlyList<T>>> Core(IEnumerable<Task<Result<T>>> results) =>
            CombineCore(await Task.WhenAll(results).ConfigureAwait(ResultConfiguration.ContinueOnCapturedContext));
    }

    /// <summary>Waits for all value tasks and combines their results, which carry no value, into one result.</summary>
    /// <param name="results">The value tasks that produce the results. They should already be running.</param>
    /// <returns>A value task that produces the combined result. See <see cref="Combine(IEnumerable{Result})"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="results"/> is <see langword="null"/>.</exception>
    /// <remarks>Every value task is awaited exactly once, so none is left unobserved even when another one throws.</remarks>
    public static ValueTask<Result> Combine(IEnumerable<ValueTask<Result>> results)
    {
        Guard.NotNull(results, nameof(results));

        return new ValueTask<Result>(Combine(results.Select(task => task.AsTask())));
    }

    /// <summary>Waits for all value tasks and combines their results, which carry a value, into one result that carries the list of the values.</summary>
    /// <typeparam name="T">The type of the values.</typeparam>
    /// <param name="results">The value tasks that produce the results. They should already be running.</param>
    /// <returns>A value task that produces the combined result. See <see cref="Combine{T}(IEnumerable{Result{T}})"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="results"/> is <see langword="null"/>.</exception>
    /// <remarks>Every value task is awaited exactly once, so none is left unobserved even when another one throws.</remarks>
    public static ValueTask<Result<IReadOnlyList<T>>> Combine<T>(IEnumerable<ValueTask<Result<T>>> results)
    {
        Guard.NotNull(results, nameof(results));

        return new ValueTask<Result<IReadOnlyList<T>>>(Combine(results.Select(task => task.AsTask())));
    }

    private static Result CombineCore(IEnumerable<Result> results)
    {
        List<Error>? errors = null;

        foreach (var result in results)
        {
            if (result.TryGetError(out var error))
            {
                errors ??= [];
                errors.Add(error);
            }
        }

        return errors is null ? Success() : Failure(Error.Aggregate(errors));
    }

    private static Result<IReadOnlyList<T>> CombineCore<T>(IEnumerable<Result<T>> results)
    {
        var values = new List<T>();
        List<Error>? errors = null;

        foreach (var result in results)
        {
            var failure = result.Unpack(out var value);

            if (failure is null)
            {
                values.Add(value);
            }
            else
            {
                errors ??= [];
                errors.Add(failure);
            }
        }

        return errors is null ? Success<IReadOnlyList<T>>(values) : Failure<IReadOnlyList<T>>(Error.Aggregate(errors));
    }
}
