namespace Wyzely.Functional.MaybeSamples;

// SCENARIO: change how maybes are shown, what GetValue() says, whether values may reach logs, and how the library awaits.
//
// WHY: everything that is a matter of taste or environment is a setting with a default, so nothing has to be configured. Settings are
// PROCESS-WIDE (maybes are created everywhere), changed atomically with MaybeConfiguration.Configure, and meant to be set once at
// start-up. This example always ends with Reset, so that it leaves the process as it found it.
public static class ConfigurationExample
{
    public static void Run(TextWriter output)
    {
        try
        {
            RunConfigured(output);
        }
        finally
        {
            MaybeConfiguration.Reset();
        }

        output.WriteLine($"After Reset: {Maybe.Some(5)}");
    }

    private static void RunConfigured(TextWriter output)
    {
        output.WriteLine($"Default some: {Maybe.Some(5)}");
        output.WriteLine($"Default none: {Maybe.None<int>()}");

        // SomeFormat ({0} is the value), NoneText and NoValueMessage (the message of the exception GetValue() throws on a none).
        MaybeConfiguration.Configure(options =>
        {
            options.SomeFormat = "Just({0})";
            options.NoneText = "Nothing";
            options.NoValueMessage = "There is nothing to read here.";
        });
        output.WriteLine($"Custom some: {Maybe.Some(5)}");
        output.WriteLine($"Custom none: {Maybe.None<int>()}");
        output.WriteLine($"Custom message: {MessageOfGetValueOnNone()}");

        // ShowValueInToString = false keeps data out of logs: the value is replaced by RedactedValue. A none has nothing to hide.
        MaybeConfiguration.Reset();
        MaybeConfiguration.Configure(options => options.ShowValueInToString = false);
        output.WriteLine($"Redacted some: {Maybe.Some("secret")}");
        output.WriteLine($"Redacted none: {Maybe.None<string>()}");

        MaybeConfiguration.Configure(options => options.RedactedValue = "[hidden]");
        output.WriteLine($"Custom redaction: {Maybe.Some("secret")}");

        // A setting is validated when it is set: a format without {0} cannot show a value.
        MaybeConfiguration.Reset();
        output.WriteLine($"A format without {{0}} is rejected: {ExceptionNameOf(() => MaybeConfiguration.Configure(options => options.SomeFormat = "Just"))}");

        // Configure applies the change atomically: when the action throws, nothing is changed.
        output.WriteLine($"A Configure that throws changes nothing: {ChangeThatThrows()}");

        ShowContinueOnCapturedContext(output);
    }

    // ContinueOnCapturedContext: by default the awaits inside the library do not return to the synchronization context that the caller
    // had (right for a library, avoids deadlocks). An application that needs the context (a UI thread) inside its own delegates can
    // switch it on. A counting context makes the difference visible: it counts how often a continuation is posted to it.
    private static void ShowContinueOnCapturedContext(TextWriter output)
    {
        var withDefault = PostsToTheContext();
        MaybeConfiguration.Configure(options => options.ContinueOnCapturedContext = true);
        var withContext = PostsToTheContext();

        output.WriteLine($"ContinueOnCapturedContext false, continuations posted to the context: {withDefault}");
        output.WriteLine($"ContinueOnCapturedContext true, continuations posted to the context: {(withContext > 0 ? "more than 0" : "0")}");
    }

    private static int PostsToTheContext()
    {
        var original = SynchronizationContext.Current;
        var context = new CountingSynchronizationContext();

        try
        {
            SynchronizationContext.SetSynchronizationContext(context);
            _ = SlowAsync().Map(number => number + 1).GetAwaiter().GetResult();
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(original);
        }

        return context.Posts;
    }

    // A receiver that is not completed yet, so that the library has to await it. ConfigureAwait(false) keeps this helper out of the count.
    private static async Task<Maybe<int>> SlowAsync()
    {
        await Task.Delay(20).ConfigureAwait(false);

        return Maybe.Some(1);
    }

    private static string ChangeThatThrows()
    {
        try
        {
            MaybeConfiguration.Configure(options =>
            {
                options.SomeFormat = "Changed({0})";

                throw new InvalidOperationException("The change fails half way.");
            });
        }
        catch (InvalidOperationException)
        {
            // Expected: the point is what the settings look like afterwards.
        }

        return Maybe.Some(5).ToString();
    }

    private static string MessageOfGetValueOnNone()
    {
        try
        {
            return Maybe.None<int>().GetValue().ToString(System.Globalization.CultureInfo.InvariantCulture);
        }
        catch (InvalidOperationException exception)
        {
            return exception.Message;
        }
    }

    private static string ExceptionNameOf(Action action)
    {
        try
        {
            action();

            return "nothing";
        }
        catch (ArgumentException exception)
        {
            return exception.GetType().Name;
        }
    }

    private sealed class CountingSynchronizationContext : SynchronizationContext
    {
        private int _posts;

        internal int Posts => Volatile.Read(ref _posts);

        public override void Post(SendOrPostCallback d, object? state)
        {
            _ = Interlocked.Increment(ref _posts);
            _ = ThreadPool.QueueUserWorkItem(_ => d(state));
        }
    }
}
