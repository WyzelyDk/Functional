namespace Wyzely.Functional;

/// <content>
/// <c>CombineInOrder</c>: like <c>Combine</c> for a sequence of tasks, but one task at a time. <c>Combine</c> starts every task of a lazy sequence
/// before it waits for any of them; these start the next task only when the previous one has finished, so that the work runs in the order of the
/// sequence.
/// </content>
public static partial class ResultCollectionExtensions
{
    /// <summary>
    /// Runs the tasks of a sequence one after another, in order, and combines their results, which carry no value, into one result. See
    /// <see cref="Combine(IEnumerable{Result})"/>.
    /// </summary>
    /// <param name="results">
    /// The tasks that produce the results. A lazy sequence creates a task when the previous one has finished, which is what makes the work run in order.
    /// </param>
    /// <returns>A task that produces the combined result. Every result is looked at: there is no short-circuit.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="results"/> is <see langword="null"/>.</exception>
    /// <remarks>An exception thrown by a task is not turned into a result: it escapes from the returned task, and the tasks after it do not run.</remarks>
    public static Task<Result> CombineInOrder(this IEnumerable<Task<Result>> results)
    {
        Guard.NotNull(results, nameof(results));

        return Core(results);

        static async Task<Result> Core(IEnumerable<Task<Result>> items)
        {
            var outcomes = new List<Result>();

            foreach (var task in items)
            {
                outcomes.Add(await task.ConfigureAwait(ResultConfiguration.ContinueOnCapturedContext));
            }

            return Result.Combine(outcomes);
        }
    }

    /// <summary>
    /// Runs the tasks of a sequence one after another, in order, and combines their results into one result with the list of the values. See
    /// <see cref="Combine{T}(IEnumerable{Result{T}})"/>.
    /// </summary>
    /// <typeparam name="T">The type of the values.</typeparam>
    /// <param name="results">
    /// The tasks that produce the results. A lazy sequence creates a task when the previous one has finished, which is what makes the work run in order.
    /// </param>
    /// <returns>A task that produces the combined result, with the values in the order of the sequence.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="results"/> is <see langword="null"/>.</exception>
    /// <remarks>An exception thrown by a task is not turned into a result: it escapes from the returned task, and the tasks after it do not run.</remarks>
    public static Task<Result<IReadOnlyList<T>>> CombineInOrder<T>(this IEnumerable<Task<Result<T>>> results)
    {
        Guard.NotNull(results, nameof(results));

        return Core(results);

        static async Task<Result<IReadOnlyList<T>>> Core(IEnumerable<Task<Result<T>>> items)
        {
            var outcomes = new List<Result<T>>();

            foreach (var task in items)
            {
                outcomes.Add(await task.ConfigureAwait(ResultConfiguration.ContinueOnCapturedContext));
            }

            return Result.Combine(outcomes);
        }
    }

    /// <summary>
    /// Runs the value tasks of a sequence one after another, in order, and combines their results, which carry no value, into one result.
    /// See <see cref="CombineInOrder(IEnumerable{Task{Result}})"/>.
    /// </summary>
    /// <param name="results">The value tasks that produce the results. A lazy sequence creates a value task when the previous one has finished.</param>
    /// <returns>A value task that produces the combined result.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="results"/> is <see langword="null"/>.</exception>
    public static ValueTask<Result> CombineInOrder(this IEnumerable<ValueTask<Result>> results)
    {
        Guard.NotNull(results, nameof(results));

        return Core(results);

        static async ValueTask<Result> Core(IEnumerable<ValueTask<Result>> items)
        {
            var outcomes = new List<Result>();

            foreach (var task in items)
            {
                outcomes.Add(await task.ConfigureAwait(ResultConfiguration.ContinueOnCapturedContext));
            }

            return Result.Combine(outcomes);
        }
    }

    /// <summary>
    /// Runs the value tasks of a sequence one after another, in order, and combines their results into one result with the list of the values.
    /// See <see cref="CombineInOrder{T}(IEnumerable{Task{Result{T}}})"/>.
    /// </summary>
    /// <typeparam name="T">The type of the values.</typeparam>
    /// <param name="results">The value tasks that produce the results. A lazy sequence creates a value task when the previous one has finished.</param>
    /// <returns>A value task that produces the combined result, with the values in the order of the sequence.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="results"/> is <see langword="null"/>.</exception>
    public static ValueTask<Result<IReadOnlyList<T>>> CombineInOrder<T>(this IEnumerable<ValueTask<Result<T>>> results)
    {
        Guard.NotNull(results, nameof(results));

        return Core(results);

        static async ValueTask<Result<IReadOnlyList<T>>> Core(IEnumerable<ValueTask<Result<T>>> items)
        {
            var outcomes = new List<Result<T>>();

            foreach (var task in items)
            {
                outcomes.Add(await task.ConfigureAwait(ResultConfiguration.ContinueOnCapturedContext));
            }

            return Result.Combine(outcomes);
        }
    }
}
