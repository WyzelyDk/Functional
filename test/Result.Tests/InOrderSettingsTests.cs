namespace Wyzely.Functional.ResultTests;

// CombineInOrder respects ContinueOnCapturedContext like every other await of the library: it does not capture the context by default and does when
// the setting says so.
[Collection(ProcessWideConfiguration.Name)]
public sealed class InOrderSettingsTests : IDisposable
{
    public void Dispose() => ResultConfiguration.Reset();

    [Fact]
    internal async Task CombineInOrder_of_pending_tasks_follows_the_setting()
    {
        var before = await PostsAsync(static () => CombineAsync());
        ResultConfiguration.Configure(static options => options.ContinueOnCapturedContext = true);
        var after = await PostsAsync(static () => CombineAsync());

        Assert.Multiple(
            () => Assert.Equal(0, before),
            () => Assert.True(after > 0));
    }

    [Fact]
    internal async Task FirstFailureOrSuccess_of_a_pending_stream_follows_the_setting()
    {
        var before = await PostsAsync(static () => FirstFailureAsync());
        ResultConfiguration.Configure(static options => options.ContinueOnCapturedContext = true);
        var after = await PostsAsync(static () => FirstFailureAsync());

        Assert.Multiple(
            () => Assert.Equal(0, before),
            () => Assert.True(after > 0));
    }

    private static async Task<Result> PendingAsync()
    {
        await Task.Delay(1).ConfigureAwait(false);

        return Result.Success();
    }

    private static Task<Result> CombineAsync() => new[] { PendingAsync(), PendingAsync() }.CombineInOrder();

    private static Task<Result> FirstFailureAsync() =>
        AsyncStreams.Pending(Result.Success(), Result.Success()).FirstFailureOrSuccess(TestContext.Current.CancellationToken).AsTask();

    // Runs one operation with a context that counts the continuations posted to it. The tasks complete later, so every await of the library is a
    // real one.
    private static async Task<int> PostsAsync(Func<Task> run)
    {
        var previous = SynchronizationContext.Current;
        var context = new PostingContext();
        SynchronizationContext.SetSynchronizationContext(context);

        try
        {
            await run().ConfigureAwait(false);
            context.WaitUntilIdle();

            return context.Posts;
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(previous);
        }
    }
}
