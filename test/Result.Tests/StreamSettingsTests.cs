namespace Wyzely.Functional.ResultTests;

// The operations on a stream respect ContinueOnCapturedContext like every other await of the library: they do not capture the context by default
// and do when the setting says so.
[Collection(ProcessWideConfiguration.Name)]
public sealed class StreamSettingsTests : IDisposable
{
    public void Dispose() => ResultConfiguration.Reset();

    [Fact]
    internal async Task Combine_of_a_pending_stream_does_not_capture_the_context_by_default()
    {
        var posts = await PostsAsync(static () => CombineAsync());

        Assert.Equal(0, posts);
    }

    [Fact]
    internal async Task Combine_of_a_pending_stream_captures_the_context_when_the_setting_says_so()
    {
        ResultConfiguration.Configure(static options => options.ContinueOnCapturedContext = true);

        var posts = await PostsAsync(static () => CombineAsync());

        Assert.True(posts > 0);
    }

    [Fact]
    internal async Task Partition_of_a_pending_stream_follows_the_setting()
    {
        var before = await PostsAsync(static () => AsyncStreams.Pending(Result.Success(1)).Partition(TestContext.Current.CancellationToken).AsTask());
        ResultConfiguration.Configure(static options => options.ContinueOnCapturedContext = true);
        var after = await PostsAsync(static () => AsyncStreams.Pending(Result.Success(1)).Partition(TestContext.Current.CancellationToken).AsTask());

        Assert.Multiple(
            () => Assert.Equal(0, before),
            () => Assert.True(after > 0));
    }

    [Fact]
    internal async Task SelectEach_with_a_pending_selector_follows_the_setting()
    {
        var before = await PostsAsync(static () => ReadAsync());
        ResultConfiguration.Configure(static options => options.ContinueOnCapturedContext = true);
        var after = await PostsAsync(static () => ReadAsync());

        Assert.Multiple(
            () => Assert.Equal(0, before),
            () => Assert.True(after > 0));
    }

    private static Task<Result<IReadOnlyList<int>>> CombineAsync() =>
        AsyncStreams.Pending(Result.Success(1), Result.Success(2)).Combine(TestContext.Current.CancellationToken).AsTask();

    private static Task<List<Result<int>>> ReadAsync() => AsyncStreams.ToListAsync(
        Result.Success<IReadOnlyList<int>>([1]).SelectEach(static async item =>
        {
            await Task.Delay(1).ConfigureAwait(false);

            return Result.Success(item);
        }),
        TestContext.Current.CancellationToken);

    // Runs one operation with a context that counts the continuations posted to it. The items arrive later, so every await of the library is a
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
