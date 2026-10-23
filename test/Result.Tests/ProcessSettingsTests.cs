using System.Diagnostics.CodeAnalysis;

namespace Wyzely.Functional.ResultTests;

// A setting that is made in a process must be respected everywhere it applies: by every await in the library, and by every thread.
[Collection(ProcessWideConfiguration.Name)]
public sealed class ProcessSettingsTests : IDisposable
{
    public void Dispose() => ResultConfiguration.Reset();

    [Fact]
    internal async Task A_pending_task_receiver_and_a_pending_task_delegate_do_not_capture_the_context_by_default()
    {
        var posts = await PostsAsync(static (receiver, next) => receiver.Bind(next));

        Assert.Equal(0, posts);
    }

    [Fact]
    internal async Task A_pending_task_receiver_and_a_pending_task_delegate_capture_the_context_twice_when_the_setting_says_so()
    {
        ResultConfiguration.Configure(static options => options.ContinueOnCapturedContext = true);

        var posts = await PostsAsync(static (receiver, next) => receiver.Bind(next));

        Assert.Equal(2, posts);
    }

    [Fact]
    internal async Task A_pending_value_task_receiver_and_a_pending_value_task_delegate_follow_the_setting()
    {
        var before = await PostsAsync(static (receiver, next) => new ValueTask<Result<int>>(receiver).Bind(x => new ValueTask<Result<int>>(next(x))).AsTask());
        ResultConfiguration.Configure(static options => options.ContinueOnCapturedContext = true);
        var after = await PostsAsync(static (receiver, next) => new ValueTask<Result<int>>(receiver).Bind(x => new ValueTask<Result<int>>(next(x))).AsTask());

        Assert.Multiple(
            () => Assert.Equal(0, before),
            () => Assert.Equal(2, after));
    }

    [Fact]
    [SuppressMessage("Usage", "xUnit1051", Justification = "The default (none) cancellation token is part of what these tests exercise.")]
    internal async Task A_setting_made_on_one_thread_is_seen_by_another()
    {
        await Task.Run(static () => ResultConfiguration.Configure(static options => options.RedactedValue = "CONFIGURED"));

        var seen = await Task.Run(static () => new ErrorDetail("k", "v", isSensitive: true).ToString());

        Assert.Equal("k=CONFIGURED", seen);
    }

    // Runs one chain with a context that counts the continuations posted to it. The receiver and the delegate both complete later, so every
    // await of the library is a real one.
    private static async Task<int> PostsAsync(Func<Task<Result<int>>, Func<int, Task<Result<int>>>, Task<Result<int>>> run)
    {
        var previous = SynchronizationContext.Current;
        var context = new PostingContext();
        SynchronizationContext.SetSynchronizationContext(context);

        try
        {
            var receiver = new TaskCompletionSource<Result<int>>(TaskCreationOptions.RunContinuationsAsynchronously);
            var inner = new TaskCompletionSource<Result<int>>(TaskCreationOptions.RunContinuationsAsynchronously);
            var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

            var task = run(
                receiver.Task,
                _ =>
                {
                    entered.SetResult();

                    return inner.Task;
                });

            receiver.SetResult(Result.Success(1));
            await entered.Task.ConfigureAwait(false);

            // The callback that ran the delegate is still busy until the library has awaited the task that the delegate returned. Completing
            // that task earlier would let the await finish synchronously, without a post, and the count would depend on timing.
            context.WaitUntilIdle();
            inner.SetResult(Result.Success(1));
            await task.ConfigureAwait(false);

            return context.Posts;
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(previous);
        }
    }
}
