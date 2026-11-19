using System.Threading.Tasks.Sources;

namespace Wyzely.Functional.ResultTests;

// Combine over value tasks: the order of the results, the wait for every task, what escapes, the setting, and that every value task is consumed once.
[Collection(ProcessWideConfiguration.Name)]
public sealed class CombineValueTasksTests : IDisposable
{
    private static readonly Error First = Error.Create("First.");
    private static readonly Error Second = Error.Create("Second.");
    private static readonly Error Third = Error.Create("Third.");

    public void Dispose() => ResultConfiguration.Reset();

    [Fact]
    internal async Task Combine_of_nothing_succeeds_for_value_tasks_without_a_value()
    {
        var result = await Result.Combine(Array.Empty<ValueTask<Result>>());

        Assert.True(result.IsSuccess);
    }

    [Fact]
    internal async Task Combine_of_nothing_gives_an_empty_list_for_value_tasks_with_a_value()
    {
        var result = await Result.Combine(Array.Empty<ValueTask<Result<int>>>());

        Assert.Empty(result.GetValue());
    }

    [Fact]
    internal async Task Combine_keeps_the_order_of_the_values_when_completed_and_pending_value_tasks_are_mixed()
    {
        var second = new TaskCompletionSource<Result<int>>(TaskCreationOptions.RunContinuationsAsynchronously);
        var fourth = new TaskCompletionSource<Result<int>>(TaskCreationOptions.RunContinuationsAsynchronously);
        ValueTask<Result<int>>[] tasks =
        [
            new(Result.Success(1)),
            new(second.Task),
            new(Result.Success(3)),
            new(fourth.Task),
        ];

        var running = Result.Combine(tasks);
        var wasPending = !running.IsCompleted;
        fourth.SetResult(Result.Success(4));
        second.SetResult(Result.Success(2));
        var result = await running;

        Assert.Multiple(
            () => Assert.True(wasPending),
            () => Assert.Equal([1, 2, 3, 4], result.GetValue()));
    }

    [Fact]
    internal async Task Combine_keeps_the_order_of_the_errors_when_completed_and_pending_value_tasks_are_mixed()
    {
        var second = new TaskCompletionSource<Result>(TaskCreationOptions.RunContinuationsAsynchronously);
        var fourth = new TaskCompletionSource<Result>(TaskCreationOptions.RunContinuationsAsynchronously);
        ValueTask<Result>[] tasks =
        [
            new(Result.Failure(First)),
            new(second.Task),
            new(Result.Success()),
            new(fourth.Task),
        ];

        var running = Result.Combine(tasks);
        fourth.SetResult(Result.Failure(Third));
        second.SetResult(Result.Failure(Second));
        var result = await running;

        Assert.Equal([First, Second, Third], result.GetError().Flatten());
    }

    [Fact]
    internal async Task Combine_returns_the_failures_of_value_tasks_with_a_value_in_the_order_of_the_sequence()
    {
        var pending = new TaskCompletionSource<Result<int>>(TaskCreationOptions.RunContinuationsAsynchronously);
        ValueTask<Result<int>>[] tasks = [new(pending.Task), new(Result.Success(2)), new(Result.Failure<int>(Second))];

        var running = Result.Combine(tasks);
        pending.SetResult(Result.Failure<int>(First));
        var result = await running;

        Assert.Equal([First, Second], result.GetError().Flatten());
    }

    [Fact]
    internal async Task Combine_waits_for_every_value_task_before_it_lets_the_exception_of_a_faulted_one_escape()
    {
        var pending = new TaskCompletionSource<Result>(TaskCreationOptions.RunContinuationsAsynchronously);
        var faulted = new ValueTask<Result>(Task.FromException<Result>(new InvalidOperationException("boom")));

        var running = Result.Combine([faulted, new ValueTask<Result>(pending.Task)]);
        var wasWaiting = !running.IsCompleted;
        pending.SetResult(Result.Success());

        Assert.True(wasWaiting);
        await Assert.ThrowsAsync<InvalidOperationException>(running.AsTask);
    }

    [Fact]
    internal async Task Combine_lets_the_exception_of_a_faulted_value_task_with_a_value_escape()
    {
        var faulted = new ValueTask<Result<int>>(Task.FromException<Result<int>>(new InvalidOperationException("boom")));

        await Assert.ThrowsAsync<InvalidOperationException>(() => Result.Combine([new ValueTask<Result<int>>(Result.Success(1)), faulted]).AsTask());
    }

    [Fact]
    internal async Task Combine_lets_the_cancellation_of_a_value_task_escape()
    {
        var canceled = new ValueTask<Result>(Task.FromCanceled<Result>(new CancellationToken(canceled: true)));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Result.Combine([canceled]).AsTask());
    }

    [Fact]
    internal async Task Combine_delivers_the_exception_of_a_sequence_that_throws_through_the_returned_task()
    {
        var running = Result.Combine(Throwing());

        await Assert.ThrowsAsync<InvalidOperationException>(running.AsTask);
    }

    [Fact]
    internal async Task Combine_delivers_the_exception_of_a_sequence_with_values_that_throws_through_the_returned_task()
    {
        var running = Result.Combine(ThrowingTyped());

        await Assert.ThrowsAsync<InvalidOperationException>(running.AsTask);
    }

    [Fact]
    internal async Task Combine_combines_value_tasks_of_a_sequence_that_does_not_know_its_size()
    {
        var pending = new TaskCompletionSource<Result<int>>(TaskCreationOptions.RunContinuationsAsynchronously);
        var source = new[] { 1, 2, 3 }.Select(
            number => number == 2 ? new ValueTask<Result<int>>(pending.Task) : new ValueTask<Result<int>>(Result.Success(number)));

        var running = Result.Combine(source);
        pending.SetResult(Result.Success(20));
        var result = await running;

        Assert.Equal([1, 20, 3], result.GetValue());
    }

    [Fact]
    internal async Task Combine_does_not_capture_the_context_to_wait_for_a_pending_value_task_by_default()
    {
        var posts = await PostsAsync();

        Assert.Equal(0, posts);
    }

    [Fact]
    internal async Task Combine_captures_the_context_once_to_wait_for_pending_value_tasks_when_the_setting_says_so()
    {
        ResultConfiguration.Configure(static options => options.ContinueOnCapturedContext = true);

        var posts = await PostsAsync();

        Assert.Equal(1, posts);
    }

    [Fact]
    internal async Task Combine_consumes_a_completed_value_task_once()
    {
        var source = new CountingSource();
        source.Complete(Result.Success(7));

        var result = await Result.Combine([source.AsValueTask()]);

        Assert.Multiple(
            () => Assert.Equal([7], result.GetValue()),
            () => Assert.Equal(1, source.Reads));
    }

    [Fact]
    internal async Task Combine_consumes_a_pending_value_task_once()
    {
        var source = new CountingSource();

        var running = Result.Combine([source.AsValueTask()]);
        var wasPending = !running.IsCompleted;
        source.Complete(Result.Success(7));
        var result = await running;

        Assert.Multiple(
            () => Assert.True(wasPending),
            () => Assert.Equal([7], result.GetValue()),
            () => Assert.Equal(1, source.Reads));
    }

    private static IEnumerable<ValueTask<Result>> Throwing()
    {
        yield return new ValueTask<Result>(Result.Success());

        throw new InvalidOperationException("The sequence broke.");
    }

    private static IEnumerable<ValueTask<Result<int>>> ThrowingTyped()
    {
        yield return new ValueTask<Result<int>>(Result.Success(1));

        throw new InvalidOperationException("The sequence broke.");
    }

    // Runs one Combine with a context that counts the continuations posted to it. The value task completes later, so the await of the library is a real one.
    private static async Task<int> PostsAsync()
    {
        var previous = SynchronizationContext.Current;
        var context = new PostingContext();
        SynchronizationContext.SetSynchronizationContext(context);

        try
        {
            var pending = new TaskCompletionSource<Result>(TaskCreationOptions.RunContinuationsAsynchronously);
            var running = Result.Combine([new ValueTask<Result>(pending.Task)]);
            var wasPending = !running.IsCompleted;
            pending.SetResult(Result.Success());
            await running.AsTask().ConfigureAwait(false);
            Assert.True(wasPending);
            context.WaitUntilIdle();

            return context.Posts;
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(previous);
        }
    }

    // A value task that is backed by a source, which counts how often its result is read.
    private sealed class CountingSource : IValueTaskSource<Result<int>>
    {
        private ManualResetValueTaskSourceCore<Result<int>> _core = default;

        internal int Reads { get; private set; }

        public Result<int> GetResult(short token)
        {
            Reads++;

            return _core.GetResult(token);
        }

        public ValueTaskSourceStatus GetStatus(short token) => _core.GetStatus(token);

        public void OnCompleted(Action<object?> continuation, object? state, short token, ValueTaskSourceOnCompletedFlags flags) =>
            _core.OnCompleted(continuation, state, token, flags);

        internal ValueTask<Result<int>> AsValueTask() => new(this, _core.Version);

        internal void Complete(Result<int> result) => _core.SetResult(result);
    }
}
