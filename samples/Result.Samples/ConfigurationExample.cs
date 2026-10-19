// CONFIGURATION: ResultConfiguration and ResultOptions.
//
// Scenario: your application wants its own wording (a different separator, a localized message, "[hidden]"
// instead of "***") or its own limit for parallel work, set once at start-up.
//
// HOW IT WORKS:
//  - Nothing has to be configured: every setting has a sensible default.
//  - You call ResultConfiguration.Configure(options => ...) once at start-up. Your action receives a COPY of the
//    current settings, and they become active when it returns. A reference you keep to that copy can never change
//    the active settings later.
//  - The settings are process-wide, on purpose: results and errors are created everywhere, and passing an options
//    object to every call would defeat a lightweight result type.
//  - Text that an Error has already rendered is not rendered again, so configure BEFORE the first error is created.
//  - ResultConfiguration.Reset() restores every default. It is mainly useful in tests, and this example uses it.
//
// The nine settings are: AggregateMessage, ChildSeparator, ContextSeparator, CancelledMessage, RedactedValue,
// ExceptionMapper, ShouldCatch (both shown in ExceptionsExample), DefaultMaxDegreeOfParallelism and
// ContinueOnCapturedContext (whether the library's awaits continue on the captured synchronization context; false
// is the default and the right choice for libraries, so it is not demonstrated).
namespace Wyzely.Functional.ResultSamples;

public static class ConfigurationExample
{
    private static readonly int[] Items = [1, 2, 3, 4, 5, 6];

    public static void Run(TextWriter output) => RunAsync(output).GetAwaiter().GetResult();

    public static async Task RunAsync(TextWriter output)
    {
        var name = Error.Validation("Name is missing.");
        var email = Error.Validation("Email is invalid.");

        // With the defaults. Reading Message renders the text, and the rendered text is kept.
        var early = Error.Aggregate([name, email]);
        await output.WriteLineAsync($"Defaults: {early.Message}");

        try
        {
            ResultConfiguration.Configure(options =>
            {
                options.ChildSeparator = " | ";
                options.ContextSeparator = " > ";
                options.RedactedValue = "[hidden]";
                options.AggregateMessage = "Several problems occurred.";
                options.CancelledMessage = "Cancelled by the user.";
            });

            await output.WriteLineAsync($"ChildSeparator ' | ': {Error.Aggregate([name, email]).Message}");

            var framed = Error.NotFound("Order {OrderId} was not found.").WithDetail("OrderId", 7).WithContext("Checkout").WithContext("Web");
            await output.WriteLineAsync($"ContextSeparator ' > ': {framed}");

            var secret = Error.NotFound("Order {OrderId} of {Customer} was not found.")
                .WithDetail("OrderId", 7)
                .WithDetail("Customer", "anna@example.com", isSensitive: true);
            await output.WriteLineAsync($"RedactedValue '[hidden]': {secret.RedactedMessage}");

            await output.WriteLineAsync($"AggregateMessage: {Error.Aggregate([name, email]).Template}");
            await output.WriteLineAsync($"CancelledMessage: {Error.Cancelled().Message}");

            // The error that was rendered before the change keeps the text it had.
            await output.WriteLineAsync($"An error rendered before the change keeps its text: {early.Message}");

            // Keeping the copy that Configure hands you does not give you a handle on the active settings.
            ResultOptions? kept = null;
            ResultConfiguration.Configure(options => kept = options);

            kept?.ChildSeparator = "!!!";

            await output.WriteLineAsync($"A kept options object changes nothing: {Error.Aggregate([name, email]).Message}");

            // The default degree of parallelism is used when the caller passes no limit of their own.
            ResultConfiguration.Configure(options => options.DefaultMaxDegreeOfParallelism = 2);
            var gauge = new ConcurrencyGauge();
            _ = await Items.SelectInParallel(
                async (item, token) =>
                {
                    gauge.Enter();

                    try
                    {
                        await Task.Delay(5, token);

                        return Result.Success(item);
                    }
                    finally
                    {
                        gauge.Exit();
                    }
                });
            await output.WriteLineAsync($"DefaultMaxDegreeOfParallelism 2, never more than 2 at the same time: {gauge.Peak <= 2}");
        }
        finally
        {
            ResultConfiguration.Reset();
        }

        await output.WriteLineAsync($"After Reset: {Error.Aggregate([name, email]).Message}");
    }
}
