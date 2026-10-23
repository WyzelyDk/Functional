namespace Wyzely.Functional.ResultTests;

// The helpers for sequences of results exist for an asynchronous stream too: Combine and Partition collect it, SelectEach turns a result of a
// collection into a stream of results, and the parallel helpers run an operation for every item. The stream is read once, in order.
public sealed class ResultAsyncStreamTests
{
    private static readonly Error First = Error.Create("First.");
    private static readonly Error Second = Error.Create("Second.");

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    [Fact]
    internal async Task Combine_collects_the_values_of_a_stream_in_order()
    {
        var outcome = await AsyncStreams.Of(Result.Success(1), Result.Success(2), Result.Success(3)).Combine(Token);

        Assert.Equal([1, 2, 3], outcome.GetValue());
    }

    [Fact]
    internal async Task Combine_returns_the_error_of_a_single_failure()
    {
        var outcome = await AsyncStreams.Of(Result.Success(1), Result.Failure<int>(First), Result.Success(3)).Combine(Token);

        Assert.Same(First, outcome.GetError());
    }

    [Fact]
    internal async Task Combine_aggregates_the_errors_of_several_failures_in_the_order_of_the_stream()
    {
        var outcome = await AsyncStreams.Of(Result.Failure<int>(First), Result.Success(2), Result.Failure<int>(Second)).Combine(Token);

        var error = outcome.GetError();

        Assert.Multiple(
            () => Assert.True(error.IsAggregate),
            () => Assert.Equal([First, Second], error.Children));
    }

    [Fact]
    internal async Task Combine_of_an_empty_stream_succeeds_with_no_values()
    {
        var outcome = await AsyncStreams.Of<Result<int>>().Combine(Token);

        Assert.Empty(outcome.GetValue());
    }

    [Fact]
    internal async Task Combine_reads_every_item_of_the_stream_once()
    {
        var pulled = 0;

        await AsyncStreams.Counted(count => pulled = count, Result.Success(1), Result.Success(2), Result.Success(3)).Combine(Token);

        Assert.Equal(3, pulled);
    }

    [Fact]
    internal async Task Combine_of_a_stream_of_results_without_a_value_aggregates_the_failures()
    {
        var success = await AsyncStreams.Of(Result.Success(), Result.Success()).Combine(Token);
        var failure = await AsyncStreams.Of(Result.Success(), Result.Failure(First)).Combine(Token);

        Assert.Multiple(
            () => Assert.True(success.IsSuccess),
            () => Assert.Same(First, failure.GetError()));
    }

    [Fact]
    internal async Task Combine_stops_when_the_token_is_cancelled()
    {
        using var source = CancellationTokenSource.CreateLinkedTokenSource(Token);
        await source.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            async () => await AsyncStreams.Cancellable([Result.Success(1)]).Combine(source.Token));
    }

    [Fact]
    internal void Combine_throws_when_the_stream_is_null()
    {
        var exception = Record.Exception(() => ((IAsyncEnumerable<Result<int>>)null!).Combine(Token));

        Assert.Equal("results", Assert.IsType<ArgumentNullException>(exception).ParamName);
    }

    [Fact]
    internal async Task Partition_splits_a_stream_into_values_and_errors_in_order()
    {
        var (values, errors) = await AsyncStreams.Of(
            Result.Success(1),
            Result.Failure<int>(First),
            Result.Success(3),
            Result.Failure<int>(Second)).Partition(Token);

        Assert.Multiple(
            () => Assert.Equal([1, 3], values),
            () => Assert.Equal([First, Second], errors));
    }

    [Fact]
    internal void Partition_throws_when_the_stream_is_null()
    {
        var exception = Record.Exception(() => ((IAsyncEnumerable<Result<int>>)null!).Partition(Token));

        Assert.Equal("results", Assert.IsType<ArgumentNullException>(exception).ParamName);
    }

    [Fact]
    internal async Task SelectEach_yields_one_result_for_every_item()
    {
        var results = await AsyncStreams.ToListAsync(
            Result.Success<IReadOnlyList<int>>([1, 2, 3]).SelectEach(static item => Result.Success(item * 10)),
            Token);

        Assert.Equal([10, 20, 30], results.Select(static result => result.GetValue()));
    }

    [Fact]
    internal async Task SelectEach_awaits_a_selector_that_returns_a_task()
    {
        var results = await AsyncStreams.ToListAsync(
            Result.Success<IReadOnlyList<int>>([1, 2]).SelectEach(static async item =>
            {
                await Task.Yield();

                return Result.Success(item * 10);
            }),
            Token);

        Assert.Equal([10, 20], results.Select(static result => result.GetValue()));
    }

    [Fact]
    internal async Task SelectEach_awaits_a_selector_that_returns_a_value_task()
    {
        var results = await AsyncStreams.ToListAsync(
            Result.Success<IReadOnlyList<int>>([1, 2]).SelectEach(static item => new ValueTask<Result<int>>(Result.Success(item * 10))),
            Token);

        Assert.Equal([10, 20], results.Select(static result => result.GetValue()));
    }

    [Fact]
    internal async Task SelectEach_accepts_a_result_of_a_plain_sequence()
    {
        var results = await AsyncStreams.ToListAsync(
            Result.Success<IEnumerable<int>>([1, 2]).SelectEach(static item => Result.Success(item + 1)),
            Token);

        Assert.Equal([2, 3], results.Select(static result => result.GetValue()));
    }

    [Fact]
    internal async Task SelectEach_yields_the_failure_of_the_receiver_once_without_calling_the_selector()
    {
        var calls = 0;

        var results = await AsyncStreams.ToListAsync(
            Result.Failure<IReadOnlyList<int>>(First).SelectEach(item =>
            {
                calls++;

                return Result.Success(item);
            }),
            Token);

        Assert.Multiple(
            () => Assert.Same(First, Assert.Single(results).GetError()),
            () => Assert.Equal(0, calls));
    }

    [Fact]
    internal async Task SelectEach_keeps_going_after_a_failed_item()
    {
        var results = await AsyncStreams.ToListAsync(
            Result.Success<IReadOnlyList<int>>([1, 2, 3]).SelectEach(static item => item == 2 ? Result.Failure<int>(First) : Result.Success(item)),
            Token);

        Assert.Equal([true, false, true], results.Select(static result => result.IsSuccess));
    }

    [Fact]
    internal async Task SelectEach_calls_the_selector_only_for_the_items_that_are_read()
    {
        var calls = 0;
        var stream = Result.Success<IReadOnlyList<int>>([1, 2, 3]).SelectEach(item =>
        {
            calls++;

            return Result.Success(item);
        });

        await using var enumerator = stream.GetAsyncEnumerator(Token);
        await enumerator.MoveNextAsync();

        Assert.Equal(1, calls);
    }

    [Fact]
    internal async Task SelectEach_stops_when_the_token_of_the_consumer_is_cancelled()
    {
        using var source = CancellationTokenSource.CreateLinkedTokenSource(Token);
        await source.CancelAsync();
        var stream = Result.Success<IReadOnlyList<int>>([1, 2]).SelectEach(static item => Result.Success(item));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await AsyncStreams.ToListAsync(stream, source.Token));
    }

    [Fact]
    internal void SelectEach_throws_when_the_selector_is_null()
    {
        var result = Result.Success<IReadOnlyList<int>>([1]);

        var exception = Record.Exception(() => result.SelectEach((Func<int, Result<int>>)null!));

        Assert.Equal("selector", Assert.IsType<ArgumentNullException>(exception).ParamName);
    }

    [Fact]
    internal async Task SelectInParallel_runs_an_operation_for_every_item_of_a_stream_and_keeps_the_order()
    {
        var outcome = await AsyncStreams.Of(1, 2, 3).SelectInParallel(
            static async (item, _) =>
            {
                await Task.Yield();

                return Result.Success(item * 10);
            },
            cancellationToken: Token);

        Assert.Equal([10, 20, 30], outcome.GetValue());
    }

    [Fact]
    internal async Task SelectInParallel_of_a_stream_accepts_an_operation_that_returns_a_value_task()
    {
        var outcome = await AsyncStreams.Of(1, 2).SelectInParallel(
            static (item, _) => new ValueTask<Result<int>>(Result.Success(item * 10)),
            cancellationToken: Token);

        Assert.Equal([10, 20], outcome.GetValue());
    }

    [Fact]
    internal async Task SelectInParallel_of_a_stream_aggregates_the_errors_in_the_order_of_the_stream()
    {
        var outcome = await AsyncStreams.Of(1, 2, 3).SelectInParallel(
            static (item, _) => Task.FromResult(item == 2 ? Result.Success(item) : Result.Failure<int>(Error.Create($"Failed {item}."))),
            cancellationToken: Token);

        Assert.Equal(["Failed 1.", "Failed 3."], outcome.GetError().Children.Select(static error => error.Message));
    }

    [Fact]
    internal async Task ForEachInParallel_runs_an_operation_for_every_item_of_a_stream()
    {
        var seen = new System.Collections.Concurrent.ConcurrentBag<int>();

        var outcome = await AsyncStreams.Of(1, 2, 3).ForEachInParallel(
            (item, _) =>
            {
                seen.Add(item);

                return Task.FromResult(Result.Success());
            },
            cancellationToken: Token);

        Assert.Multiple(
            () => Assert.True(outcome.IsSuccess),
            () => Assert.Equal([1, 2, 3], seen.Order()));
    }

    [Fact]
    internal async Task ForEachInParallel_of_a_stream_accepts_an_operation_that_returns_a_value_task_and_aggregates_the_errors()
    {
        var outcome = await AsyncStreams.Of(1, 2, 3).ForEachInParallel(
            static (item, _) => new ValueTask<Result>(item == 2 ? Result.Success() : Result.Failure(Error.Create($"Failed {item}."))),
            cancellationToken: Token);

        Assert.Equal(["Failed 1.", "Failed 3."], outcome.GetError().Children.Select(static error => error.Message));
    }

    [Fact]
    internal void The_parallel_helpers_throw_when_the_stream_or_the_operation_is_null_or_the_degree_is_invalid()
    {
        var stream = AsyncStreams.Of(1);

        var nullStream = Record.Exception(
            () => (object)((IAsyncEnumerable<int>)null!).SelectInParallel(static (item, _) => Task.FromResult(Result.Success(item)), cancellationToken: Token));
        var nullOperation = Record.Exception(
            () => (object)stream.SelectInParallel((Func<int, CancellationToken, Task<Result<int>>>)null!, cancellationToken: Token));
        var nullForEach = Record.Exception(
            () => (object)((IAsyncEnumerable<int>)null!).ForEachInParallel(static (_, _) => Task.FromResult(Result.Success()), cancellationToken: Token));
        var degree = Record.Exception(
            () => (object)stream.ForEachInParallel(static (_, _) => Task.FromResult(Result.Success()), maxDegreeOfParallelism: 0, cancellationToken: Token));

        Assert.Multiple(
            () => Assert.IsType<ArgumentNullException>(nullStream),
            () => Assert.IsType<ArgumentNullException>(nullOperation),
            () => Assert.IsType<ArgumentNullException>(nullForEach),
            () => Assert.IsType<ArgumentOutOfRangeException>(degree));
    }
}
