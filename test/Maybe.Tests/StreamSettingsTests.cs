namespace Wyzely.Functional.MaybeTests;

// The operations on a stream respect ContinueOnCapturedContext like every other await of the library: they do not capture the context by default
// and do when the setting says so.
[Collection(ProcessWideConfiguration.Name)]
public sealed class StreamSettingsTests : IDisposable
{
    public void Dispose() => MaybeConfiguration.Reset();

    [Fact]
    internal async Task FirstOrNone_of_a_pending_stream_follows_the_setting()
    {
        var before = await PostsAsync(static () => AsyncStreams.Pending(1, 2).FirstOrNone(TestContext.Current.CancellationToken).AsTask());
        MaybeConfiguration.Configure(static options => options.ContinueOnCapturedContext = true);
        var after = await PostsAsync(static () => AsyncStreams.Pending(1, 2).FirstOrNone(TestContext.Current.CancellationToken).AsTask());

        Assert.Multiple(
            () => Assert.Equal(0, before),
            () => Assert.True(after > 0));
    }

    [Fact]
    internal async Task LastOrNone_with_a_pending_predicate_follows_the_setting()
    {
        var before = await PostsAsync(static () => LastAsync());
        MaybeConfiguration.Configure(static options => options.ContinueOnCapturedContext = true);
        var after = await PostsAsync(static () => LastAsync());

        Assert.Multiple(
            () => Assert.Equal(0, before),
            () => Assert.True(after > 0));
    }

    [Fact]
    internal async Task Choose_of_a_pending_stream_follows_the_setting()
    {
        var before = await PostsAsync(static () => ChooseAsync());
        MaybeConfiguration.Configure(static options => options.ContinueOnCapturedContext = true);
        var after = await PostsAsync(static () => ChooseAsync());

        Assert.Multiple(
            () => Assert.Equal(0, before),
            () => Assert.True(after > 0));
    }

    private static Task<Maybe<int>> LastAsync() => AsyncStreams.Pending(1, 2).LastOrNone(
        static async item =>
        {
            await Task.Delay(1).ConfigureAwait(false);

            return item > 0;
        },
        TestContext.Current.CancellationToken).AsTask();

    private static Task<List<int>> ChooseAsync() => AsyncStreams.ToListAsync(
        AsyncStreams.Pending(Maybe.Some(1), Maybe.Some(2)).Choose(),
        TestContext.Current.CancellationToken);

    // Runs one operation with a context that counts the continuations posted to it. The items arrive later, so every await of the library is a
    // real one.
    private static async Task<int> PostsAsync(Func<Task> run)
    {
        var previous = SynchronizationContext.Current;
        var context = new CountingSynchronizationContext();
        SynchronizationContext.SetSynchronizationContext(context);

        try
        {
            await run().ConfigureAwait(false);

            return context.Posts;
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(previous);
        }
    }
}
