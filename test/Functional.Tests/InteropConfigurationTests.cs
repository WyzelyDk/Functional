namespace Wyzely.Functional.Tests;

// The interop methods await through the railway of the package that owns the receiver, so each follows that package's setting:
// ToResult follows MaybeConfiguration and ToMaybe follows ResultConfiguration.
[Collection(ProcessWideConfiguration.Name)]
public sealed class InteropConfigurationTests : IDisposable
{
    public void Dispose()
    {
        MaybeConfiguration.Reset();
        ResultConfiguration.Reset();
    }

    [Fact]
    internal async Task ToResult_does_not_continue_on_the_captured_context_by_default()
    {
        var posts = await CountPostsAsync(source => source.Task.ToResult(Error.Create("Missing.")), Maybe.Some(1));

        Assert.Equal(0, posts);
    }

    [Fact]
    internal async Task ToResult_continues_on_the_captured_context_when_the_maybe_setting_says_so()
    {
        MaybeConfiguration.Configure(options => options.ContinueOnCapturedContext = true);

        var posts = await CountPostsAsync(source => source.Task.ToResult(Error.Create("Missing.")), Maybe.Some(1));

        Assert.Equal(1, posts);
    }

    [Fact]
    internal async Task ToResult_ignores_the_result_setting()
    {
        ResultConfiguration.Configure(options => options.ContinueOnCapturedContext = true);

        var posts = await CountPostsAsync(source => source.Task.ToResult(Error.Create("Missing.")), Maybe.Some(1));

        Assert.Equal(0, posts);
    }

    [Fact]
    internal async Task ToMaybe_does_not_continue_on_the_captured_context_by_default()
    {
        var posts = await CountPostsAsync(source => source.Task.ToMaybe(), Result.Success(1));

        Assert.Equal(0, posts);
    }

    [Fact]
    internal async Task ToMaybe_continues_on_the_captured_context_when_the_result_setting_says_so()
    {
        ResultConfiguration.Configure(options => options.ContinueOnCapturedContext = true);

        var posts = await CountPostsAsync(source => source.Task.ToMaybe(), Result.Success(1));

        Assert.Equal(1, posts);
    }

    [Fact]
    internal async Task ToMaybe_ignores_the_maybe_setting()
    {
        MaybeConfiguration.Configure(options => options.ContinueOnCapturedContext = true);

        var posts = await CountPostsAsync(source => source.Task.ToMaybe(), Result.Success(1));

        Assert.Equal(0, posts);
    }

    private static async Task<int> CountPostsAsync<TSource>(Func<TaskCompletionSource<TSource>, Task> convert, TSource completion)
    {
        var previous = SynchronizationContext.Current;
        var context = new CountingSynchronizationContext();
        SynchronizationContext.SetSynchronizationContext(context);

        try
        {
            var source = new TaskCompletionSource<TSource>(TaskCreationOptions.RunContinuationsAsynchronously);
            var converted = convert(source);
            source.SetResult(completion);
            await converted.ConfigureAwait(false);

            return context.Posts;
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(previous);
        }
    }
}
