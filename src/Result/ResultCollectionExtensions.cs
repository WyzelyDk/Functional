namespace Wyzely.Functional;

/// <summary>
/// Extension methods for sequences of results: combining them into one result and splitting them into successes and failures.
/// </summary>
public static partial class ResultCollectionExtensions
{
    /// <summary>Combines results that carry no value into one result. See <see cref="Result.Combine(IEnumerable{Result})"/>.</summary>
    /// <param name="results">The results to combine.</param>
    /// <returns>A successful result, or a failed result with the error (or the aggregate of the errors) of the failed results.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="results"/> is <see langword="null"/>.</exception>
    public static Result Combine(this IEnumerable<Result> results) => Result.Combine(results);

    /// <summary>
    /// Combines results that carry a value into one result with the list of the values. See
    /// <see cref="Result.Combine{T}(IEnumerable{Result{T}})"/>.
    /// </summary>
    /// <typeparam name="T">The type of the values.</typeparam>
    /// <param name="results">The results to combine.</param>
    /// <returns>A successful result with the values, or a failed result with the error (or the aggregate of the errors) of the failed results.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="results"/> is <see langword="null"/>.</exception>
    public static Result<IReadOnlyList<T>> Combine<T>(this IEnumerable<Result<T>> results) => Result.Combine(results);

    /// <summary>Waits for all tasks and combines their results. See <see cref="Result.Combine(IEnumerable{Task{Result}})"/>.</summary>
    /// <param name="results">The tasks that produce the results.</param>
    /// <returns>A task that produces the combined result.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="results"/> is <see langword="null"/>.</exception>
    public static Task<Result> Combine(this IEnumerable<Task<Result>> results) => Result.Combine(results);

    /// <summary>
    /// Waits for all tasks and combines their results into one result with the list of the values. See
    /// <see cref="Result.Combine{T}(IEnumerable{Task{Result{T}}})"/>.
    /// </summary>
    /// <typeparam name="T">The type of the values.</typeparam>
    /// <param name="results">The tasks that produce the results.</param>
    /// <returns>A task that produces the combined result.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="results"/> is <see langword="null"/>.</exception>
    public static Task<Result<IReadOnlyList<T>>> Combine<T>(this IEnumerable<Task<Result<T>>> results) => Result.Combine(results);

    /// <summary>Waits for all value tasks and combines their results. See <see cref="Result.Combine(IEnumerable{ValueTask{Result}})"/>.</summary>
    /// <param name="results">The value tasks that produce the results.</param>
    /// <returns>A value task that produces the combined result.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="results"/> is <see langword="null"/>.</exception>
    public static ValueTask<Result> Combine(this IEnumerable<ValueTask<Result>> results) => Result.Combine(results);

    /// <summary>
    /// Waits for all value tasks and combines their results into one result with the list of the values. See
    /// <see cref="Result.Combine{T}(IEnumerable{ValueTask{Result{T}}})"/>.
    /// </summary>
    /// <typeparam name="T">The type of the values.</typeparam>
    /// <param name="results">The value tasks that produce the results.</param>
    /// <returns>A value task that produces the combined result.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="results"/> is <see langword="null"/>.</exception>
    public static ValueTask<Result<IReadOnlyList<T>>> Combine<T>(this IEnumerable<ValueTask<Result<T>>> results) => Result.Combine(results);

    /// <summary>Splits results into the values of the successful ones and the errors of the failed ones, both in sequence order.</summary>
    /// <typeparam name="T">The type of the values.</typeparam>
    /// <param name="results">The results to split.</param>
    /// <returns>
    /// A tuple of the values of all successful results and the errors of all failed results. Unlike <c>Combine</c>, this keeps
    /// the successes when something failed, which suits batch work where partial success is acceptable.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="results"/> is <see langword="null"/>.</exception>
    public static (IReadOnlyList<T> Values, IReadOnlyList<Error> Errors) Partition<T>(this IEnumerable<Result<T>> results)
    {
        Guard.NotNull(results, nameof(results));

        var values = new List<T>();
        var errors = new List<Error>();

        foreach (var result in results)
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
