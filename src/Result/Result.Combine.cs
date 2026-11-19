namespace Wyzely.Functional;

/// <content>
/// <c>Combine</c>: turns many results into one. Every result is looked at (there is no short-circuit),
/// so a combined failure carries the errors of all failed results, in the order of the sequence.
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
    public static Result Combine(IEnumerable<Result> results)
    {
        Guard.NotNull(results, nameof(results));

        return CombineCore(results);
    }

    /// <summary>Combines results that carry a value into one result that carries the list of the values.</summary>
    /// <typeparam name="T">The type of the values.</typeparam>
    /// <param name="results">The results to combine.</param>
    /// <returns>
    /// A successful result that holds the values in sequence order when every result succeeded (an empty list for an empty sequence).
    /// Otherwise a failed result whose error is the only error, or an aggregate of all errors in sequence order.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="results"/> is <see langword="null"/>.</exception>
    public static Result<IReadOnlyList<T>> Combine<T>(IEnumerable<Result<T>> results)
    {
        Guard.NotNull(results, nameof(results));

        return CombineCore(results);
    }

    /// <summary>Waits for all tasks and combines their results, which carry no value, into one result.</summary>
    /// <param name="results">The tasks that produce the results. They should already be running.</param>
    /// <returns>A task that produces the combined result. See <see cref="Combine(IEnumerable{Result})"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="results"/> is <see langword="null"/>.</exception>
    /// <remarks>An exception thrown by a task is not turned into a result: it escapes from the returned task.</remarks>
    public static Task<Result> Combine(IEnumerable<Task<Result>> results)
    {
        Guard.NotNull(results, nameof(results));

        return Core(results);

        static async Task<Result> Core(IEnumerable<Task<Result>> results)
        {
            var combined = await Task.WhenAll(results).ConfigureAwait(ResultConfiguration.ContinueOnCapturedContext);

            return CombineCore(combined);
        }
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

        static async Task<Result<IReadOnlyList<T>>> Core(IEnumerable<Task<Result<T>>> results)
        {
            var combined = await Task.WhenAll(results).ConfigureAwait(ResultConfiguration.ContinueOnCapturedContext);

            return CombineCore(combined);
        }
    }

    /// <summary>Waits for all value tasks and combines their results, which carry no value, into one result.</summary>
    /// <param name="results">The value tasks that produce the results. They should already be running.</param>
    /// <returns>A value task that produces the combined result. See <see cref="Combine(IEnumerable{Result})"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="results"/> is <see langword="null"/>.</exception>
    /// <remarks>
    /// Every value task is consumed exactly once, so none is left unobserved even when another one throws.
    /// A value task that has already completed is read as it is, without making a <see cref="Task"/> for it.
    /// </remarks>
    public static ValueTask<Result> Combine(IEnumerable<ValueTask<Result>> results)
    {
        Guard.NotNull(results, nameof(results));

        return CombineValueTasks<Result, Result>(results, CombineCore);
    }

    /// <summary>Waits for all value tasks and combines their results, which carry a value, into one result that carries the list of the values.</summary>
    /// <typeparam name="T">The type of the values.</typeparam>
    /// <param name="results">The value tasks that produce the results. They should already be running.</param>
    /// <returns>A value task that produces the combined result. See <see cref="Combine{T}(IEnumerable{Result{T}})"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="results"/> is <see langword="null"/>.</exception>
    /// <remarks>
    /// Every value task is consumed exactly once, so none is left unobserved even when another one throws.
    /// A value task that has already completed is read as it is, without making a <see cref="Task"/> for it.
    /// </remarks>
    public static ValueTask<Result<IReadOnlyList<T>>> Combine<T>(IEnumerable<ValueTask<Result<T>>> results)
    {
        Guard.NotNull(results, nameof(results));

        return CombineValueTasks<Result<T>, Result<IReadOnlyList<T>>>(results, CombineCore);
    }

    // A value task that has already completed successfully is read as it is, so that no Task is made for it; only the others become tasks,
    // and the call waits for those alone. Every value task is consumed once. An exception of the sequence ends up in the returned task, as it does
    // for a sequence of tasks.
    private static ValueTask<TCombined> CombineValueTasks<TResult, TCombined>(
        IEnumerable<ValueTask<TResult>> results,
        Func<List<TResult>, TCombined> combine)
    {
        try
        {
            var capacity = CapacityOf(results);
            var slots = new List<TResult>(capacity);
            List<(int Index, Task<TResult> Task)>? pending = null;

            foreach (var valueTask in results)
            {
                if (valueTask.IsCompletedSuccessfully)
                {
                    slots.Add(valueTask.Result);
                    continue;
                }

                pending ??= [];
                var task = valueTask.AsTask();
                pending.Add((slots.Count, task));
                slots.Add(default!);
            }

            if (pending is null)
            {
                var combined = combine(slots);

                return new ValueTask<TCombined>(combined);
            }

            var finishing = FinishAsync(slots, pending, combine);

            return new ValueTask<TCombined>(finishing);
        }
        catch (Exception exception)
        {
            var faulted = Task.FromException<TCombined>(exception);

            return new ValueTask<TCombined>(faulted);
        }
    }

    private static async Task<TCombined> FinishAsync<TResult, TCombined>(
        List<TResult> slots,
        List<(int Index, Task<TResult> Task)> pending,
        Func<List<TResult>, TCombined> combine)
    {
        var tasks = new Task[pending.Count];

        for (var position = 0; position < tasks.Length; position++)
        {
            tasks[position] = pending[position].Task;
        }

        await Task.WhenAll(tasks).ConfigureAwait(ResultConfiguration.ContinueOnCapturedContext);

        foreach (var (index, task) in pending)
        {
            slots[index] = await task.ConfigureAwait(ResultConfiguration.ContinueOnCapturedContext);
        }

        return combine(slots);
    }

    private static int CapacityOf<TItem>(IEnumerable<TItem> items) => items is ICollection<TItem> collection ? collection.Count : 0;

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

        if (errors is null)
        {
            return Success();
        }

        var aggregate = Error.Aggregate(errors);

        return Failure(aggregate);
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

        if (errors is null)
        {
            return Success<IReadOnlyList<T>>(values);
        }

        var aggregate = Error.Aggregate(errors);

        return Failure<IReadOnlyList<T>>(aggregate);
    }
}
