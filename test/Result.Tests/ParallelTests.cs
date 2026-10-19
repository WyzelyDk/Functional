using System.Diagnostics.CodeAnalysis;

namespace Wyzely.Functional.ResultTests;

[SuppressMessage("Usage", "xUnit1051", Justification = "The default (none) cancellation token is part of what these tests exercise.")]
public sealed class ParallelTests
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

    [Fact]
    internal async Task SelectInParallel_returns_the_values_in_the_order_of_the_source()
    {
        var result = await new[] { 1, 2, 3, 4 }.SelectInParallel(
            async (item, token) =>
            {
                await Task.Delay((5 - item) * 10, token);

                return Result.Success(item * 10);
            });

        Assert.Equal([10, 20, 30, 40], result.GetValue());
    }

    [Fact]
    internal async Task SelectInParallel_of_an_empty_source_returns_an_empty_list()
    {
        var result = await Array.Empty<int>().SelectInParallel((item, token) => Task.FromResult(Result.Success(item)));

        Assert.Empty(result.GetValue());
    }

    [Fact]
    internal async Task SelectInParallel_runs_operations_at_the_same_time()
    {
        var started = 0;
        var bothStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var result = await new[] { 1, 2 }.SelectInParallel(
            async (item, token) =>
            {
                if (Interlocked.Increment(ref started) == 2)
                {
                    bothStarted.SetResult();
                }

                await bothStarted.Task.WaitAsync(Timeout, token);

                return Result.Success(item);
            });

        Assert.True(result.IsSuccess);
    }

    [Fact]
    internal async Task SelectInParallel_without_a_limit_runs_more_operations_at_the_same_time_than_there_are_processors()
    {
        var count = Math.Max(64, 4 * Environment.ProcessorCount);
        var started = 0;
        var allStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var result = await Enumerable.Range(1, count).SelectInParallel(
            async (item, token) =>
            {
                if (Interlocked.Increment(ref started) == count)
                {
                    allStarted.SetResult();
                }

                await allStarted.Task.WaitAsync(Timeout, token);

                return Result.Success(item);
            });

        Assert.True(result.IsSuccess);
    }

    [Fact]
    internal async Task SelectInParallel_never_exceeds_the_maximum_degree_of_parallelism()
    {
        var current = 0;
        var highest = 0;

        var result = await Enumerable.Range(1, 12).SelectInParallel(
            async (item, token) =>
            {
                var now = Interlocked.Increment(ref current);
                InterlockedMax(ref highest, now);
                await Task.Delay(10, token);
                Interlocked.Decrement(ref current);

                return Result.Success(item);
            },
            maxDegreeOfParallelism: 3);

        Assert.Multiple(
            () => Assert.True(result.IsSuccess),
            () => Assert.InRange(highest, 1, 3));
    }

    [Fact]
    internal async Task SelectInParallel_attempts_every_item_and_aggregates_the_errors_in_the_order_of_the_source()
    {
        var attempted = 0;

        var result = await new[] { 1, 2, 3, 4 }.SelectInParallel(
            (item, token) =>
            {
                Interlocked.Increment(ref attempted);

                return Task.FromResult(item % 2 == 0 ? Result.Failure<int>(Error.Create("Item " + item)) : Result.Success(item));
            },
            maxDegreeOfParallelism: 1);

        Assert.Multiple(
            () => Assert.Equal(4, attempted),
            () => Assert.Equal(["Item 2", "Item 4"], result.GetError().Children.Select(child => child.Message)));
    }

    [Fact]
    internal async Task SelectInParallel_supports_value_task_operations()
    {
        var result = await new[] { 1, 2 }.SelectInParallel((item, token) => new ValueTask<Result<int>>(Result.Success(item + 1)));

        Assert.Equal([2, 3], result.GetValue());
    }

    [Fact]
    internal async Task SelectInParallel_passes_the_cancellation_token_to_the_operation()
    {
        using var source = new CancellationTokenSource();
        CancellationToken seen = default;

        await new[] { 1 }.SelectInParallel(
            (item, token) =>
            {
                seen = token;

                return Task.FromResult(Result.Success(item));
            },
            cancellationToken: source.Token);

        Assert.Equal(source.Token, seen);
    }

    [Fact]
    internal async Task SelectInParallel_throws_when_the_token_is_already_cancelled()
    {
        using var source = new CancellationTokenSource();
        await source.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => new[] { 1 }.SelectInParallel(
            (item, token) => Task.FromResult(Result.Success(item)),
            cancellationToken: source.Token));
    }

    [Fact]
    internal async Task SelectInParallel_lets_an_exception_of_the_operation_escape()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() => new[] { 1 }.SelectInParallel<int, int>(
            (item, token) => throw new InvalidOperationException("boom")));
    }

    [Fact]
    internal void SelectInParallel_rejects_an_invalid_degree_of_parallelism()
    {
        static Task<Result<int>> Succeed(int item, CancellationToken token) => Task.FromResult(Result.Success(item));

        Assert.Multiple(
            () => Assert.Throws<ArgumentOutOfRangeException>(() => { _ = new[] { 1 }.SelectInParallel(Succeed, maxDegreeOfParallelism: 0); }),
            () => Assert.Throws<ArgumentOutOfRangeException>(() => { _ = new[] { 1 }.SelectInParallel(Succeed, maxDegreeOfParallelism: -2); }));
    }

    [Fact]
    internal void SelectInParallel_throws_when_an_argument_is_null()
    {
        static Task<Result<int>> Succeed(int item, CancellationToken token) => Task.FromResult(Result.Success(item));

        Assert.Multiple(
            () => Assert.Throws<ArgumentNullException>(() => { _ = ((IEnumerable<int>)null!).SelectInParallel(Succeed); }),
            () => Assert.Throws<ArgumentNullException>(() => { _ = new[] { 1 }.SelectInParallel((Func<int, CancellationToken, Task<Result<int>>>)null!); }));
    }

    [Fact]
    internal async Task ForEachInParallel_runs_every_item_and_succeeds()
    {
        var sum = 0;

        var result = await new[] { 1, 2, 3 }.ForEachInParallel((item, token) =>
        {
            Interlocked.Add(ref sum, item);

            return Task.FromResult(Result.Success());
        });

        Assert.Multiple(
            () => Assert.True(result.IsSuccess),
            () => Assert.Equal(6, sum));
    }

    [Fact]
    internal async Task ForEachInParallel_aggregates_the_errors_in_the_order_of_the_source()
    {
        var result = await new[] { 1, 2, 3 }.ForEachInParallel(
            (item, token) => Task.FromResult(item == 2 ? Result.Success() : Result.Failure("Item " + item)),
            maxDegreeOfParallelism: 1);

        Assert.Equal(["Item 1", "Item 3"], result.GetError().Children.Select(child => child.Message));
    }

    [Fact]
    internal async Task ForEachInParallel_supports_value_task_operations()
    {
        var result = await new[] { 1 }.ForEachInParallel((item, token) => new ValueTask<Result>(Result.Failure("x")));

        Assert.True(result.IsFailure);
    }

    [Fact]
    internal async Task ForEachInParallel_of_an_empty_source_succeeds()
    {
        var result = await Array.Empty<int>().ForEachInParallel((item, token) => Task.FromResult(Result.Success()));

        Assert.True(result.IsSuccess);
    }

    private static void InterlockedMax(ref int target, int value)
    {
        int snapshot;

        do
        {
            snapshot = Volatile.Read(ref target);
        }
        while (value > snapshot && Interlocked.CompareExchange(ref target, value, snapshot) != snapshot);
    }
}
