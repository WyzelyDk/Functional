namespace Wyzely.Functional.FunctionalSamples;

// SCENARIO: an application with a UI or an old ASP.NET synchronization context wants its awaits inside the libraries to resume on the
// captured context; a service wants the default (no capture, which is faster and avoids deadlocks).
//
// WHY: there are TWO settings, one per package, and they do not know about each other. MaybeConfiguration decides how the Maybe
// package awaits, ResultConfiguration how the Result package awaits. The umbrella package adds no setting of its own: ToResult waits
// through the Maybe package, so it follows the Maybe setting, and ToMaybe waits through the Result package, so it follows the
// Result setting. Both are process-wide: set them once at start-up, and restore them in tests.
public static class ConfigurationExample
{
    public static void Run(TextWriter output) => RunAsync(output).GetAwaiter().GetResult();

    private static async Task RunAsync(TextWriter output)
    {
        try
        {
            // Each row converts a task that is NOT yet complete, so the library has to await it. We count how often the await posted
            // back to the synchronization context: 1 means "captured the context", 0 means "did not".
            await RowAsync(output, "Task<Maybe<T>>.ToResult follows Maybe ", maybe => maybe.ToResult(Error.NotFound("Missing.")), Maybe.Some(1));
            await RowAsync(output, "Task<Result<T>>.ToMaybe follows Result", result => result.ToMaybe(), Result.Success(1));
            await RowAsync(output, "Task<Maybe<T>>.Filter follows Maybe   ", maybe => maybe.Filter(value => value > 0), Maybe.Some(1));
            await RowAsync(output, "Task<Result<T>>.Ensure follows Result ", result => result.Ensure(value => value > 0, "Not positive."), Result.Success(1));
        }
        finally
        {
            MaybeConfiguration.Reset();
            ResultConfiguration.Reset();
        }

        // Reset puts both back to the defaults, which is what a test that touched the settings should do.
        var posts = await CountPostsAsync(maybe => maybe.ToResult(Error.NotFound("Missing.")), Maybe.Some(1));
        await output.WriteLineAsync($"Settings restored: {posts == 0}");
    }

    private static async Task RowAsync<TSource, TOutcome>(TextWriter output, string label, Func<Task<TSource>, Task<TOutcome>> convert, TSource completion)
    {
        MaybeConfiguration.Reset();
        ResultConfiguration.Reset();
        var byDefault = await CountPostsAsync(convert, completion);

        MaybeConfiguration.Configure(options => options.ContinueOnCapturedContext = true);
        var maybeSetting = await CountPostsAsync(convert, completion);
        MaybeConfiguration.Reset();

        ResultConfiguration.Configure(options => options.ContinueOnCapturedContext = true);
        var resultSetting = await CountPostsAsync(convert, completion);
        ResultConfiguration.Reset();

        await output.WriteLineAsync($"{label}: default {byDefault}, Maybe=true {maybeSetting}, Result=true {resultSetting}");
    }

    private static async Task<int> CountPostsAsync<TSource, TOutcome>(Func<Task<TSource>, Task<TOutcome>> convert, TSource completion)
    {
        var previous = SynchronizationContext.Current;
        var context = new CountingSynchronizationContext();
        SynchronizationContext.SetSynchronizationContext(context);

        try
        {
            var source = new TaskCompletionSource<TSource>(TaskCreationOptions.RunContinuationsAsynchronously);
            var converted = convert(source.Task);
            source.SetResult(completion);
            await converted.ConfigureAwait(false);

            return context.Posts;
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(previous);
        }
    }

    // Counts how often a continuation is posted to it. A real context (a UI thread) would run the continuation there.
    private sealed class CountingSynchronizationContext : SynchronizationContext
    {
        private int _posts;

        internal int Posts => Volatile.Read(ref _posts);

        public override void Post(SendOrPostCallback d, object? state)
        {
            Interlocked.Increment(ref _posts);
            ThreadPool.QueueUserWorkItem(_ => d(state));
        }
    }
}
