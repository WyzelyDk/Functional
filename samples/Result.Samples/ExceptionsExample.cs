// EXCEPTIONS AT THE BOUNDARY: Try, FromException, ExceptionMapper, ShouldCatch and cancellation.
//
// Scenario: your code is built on results, but it calls code that throws: int.Parse, an HTTP client, a database
// driver. Expected failures are returned as results INSIDE your code; exceptions are turned into results at the
// edge, in one place, so the rest of the code never needs a try/catch.
//
// THE RULES this library follows:
//  - Result.Try runs your code and turns an exception into a failed result that keeps the exception as its cause.
//  - Cancellation is NOT turned into a result by default: it is a request from the caller, not a failure, so an
//    OperationCanceledException keeps travelling. ShouldCatch changes that, and Error.Cancelled() exists for when
//    you want a cancellation to be a failure explicitly.
//  - The way an exception becomes an Error (the ExceptionMapper) is a setting with a sensible default,
//    Error.FromException, and can be replaced for the whole application or for a single call.
//  - A handler whose parameter names an exception type, (FormatException exception) => ..., catches only that kind (and what
//    derives from it), optionally narrowed by a `when:` condition. Naming the type is the decision, so ShouldCatch does not apply.
//  - Going the other way, ThrowIfFailure() leaves the railway as an exception for code that expects one.
//
// This example changes the process-wide ResultConfiguration for a moment and ALWAYS resets it afterwards.
namespace Wyzely.Functional.ResultSamples;

public static class ExceptionsExample
{
    public static void Run(TextWriter output) => RunAsync(output).GetAwaiter().GetResult();

    public static async Task RunAsync(TextWriter output)
    {
        // A function that returns a value, and one that throws.
        var parsed = Result.Try(() => int.Parse("42", System.Globalization.CultureInfo.InvariantCulture));
        await output.WriteLineAsync($"Try, success: {parsed.ValueOr(-1)}");

        var failed = Result.Try(() => int.Parse("abc", System.Globalization.CultureInfo.InvariantCulture));
        await output.WriteLineAsync($"Try, failure: kind {ErrorOf(failed)?.Kind}, cause {ErrorOf(failed)?.Exception?.GetType().Name}");

        // Your own mapping, for one call: a message that makes sense to the user, the exception kept as the cause.
        var quantity = Result.Try(
            () => int.Parse("abc", System.Globalization.CultureInfo.InvariantCulture),
            exception => Error.Validation("'{Text}' is not a valid quantity.").WithDetail("Text", "abc").WithException(exception));
        await output.WriteLineAsync(
            "Try with your own mapping: " +
            quantity.Match(_ => "ok", error => $"{error.Message} (cause {error.Exception?.GetType().Name})"));

        // One kind of exception, named by the parameter of the handler: the value and the exception type are both inferred.
        var narrow = Result.Try(
            () => int.Parse("abc", System.Globalization.CultureInfo.InvariantCulture),
            (FormatException exception) => Error.Validation("'{Text}' is not a number.").WithDetail("Text", "abc").WithException(exception));
        await output.WriteLineAsync(
            "Try for one kind of exception: " +
            narrow.Match(_ => "ok", error => $"{error.Message} (cause {error.Exception?.GetType().Name})"));

        // Another kind of exception keeps travelling.
        try
        {
            _ = Result.Try(
                () => int.Parse("abc", System.Globalization.CultureInfo.InvariantCulture),
                (InvalidOperationException exception) => Error.FromException(exception));
        }
        catch (FormatException)
        {
            await output.WriteLineAsync("Another kind of exception keeps travelling: FormatException");
        }

        // Naming the type is also how a cancellation is caught on purpose; `when` keeps a cancellation by the caller out of it.
        var timedOut = Result.Try(
            ThrowCancelled,
            (OperationCanceledException exception) => Error.Timeout("The request timed out.").WithException(exception),
            when: _ => !CancellationToken.None.IsCancellationRequested);
        await output.WriteLineAsync($"A named cancellation is caught: kind {ErrorOf(timedOut)?.Kind}");

        // An action that returns nothing gives an untyped Result.
        var untyped = Result.Try(Console.Out.Flush);
        await output.WriteLineAsync($"Untyped Try: {untyped}");

        // Asynchronous code: the Task and ValueTask overloads await the code for you.
        var task = await Result.Try(static async () =>
        {
            await Task.Yield();

            throw new InvalidOperationException("The warehouse is closed.");
        });
        await output.WriteLineAsync($"Async Try (Task): kind {ErrorOf(task)?.Kind}, cause {ErrorOf(task)?.Exception?.GetType().Name}");

        var valueTask = await Result.Try(static () => ThrowTimeoutAsync());
        await output.WriteLineAsync($"Async Try (ValueTask): kind {ErrorOf(valueTask)?.Kind}, transient {ErrorOf(valueTask)?.IsTransient}");

        // Cancellation keeps travelling as an exception.
        try
        {
            _ = Result.Try(ThrowCancelled);
        }
        catch (OperationCanceledException)
        {
            await output.WriteLineAsync("Cancellation is not caught by default: OperationCanceledException");
        }

        // If a cancellation should become a failure, say so explicitly...
        var cancelled = Error.Cancelled();
        await output.WriteLineAsync($"Error.Cancelled(): {cancelled.Message} (kind {cancelled.Kind})");

        // ...FromException builds the same kind of error from an exception directly...
        var fromException = Result.FromException<int>(new TimeoutException("Gateway timed out."));
        await output.WriteLineAsync($"FromException: kind {ErrorOf(fromException)?.Kind}, transient {ErrorOf(fromException)?.IsTransient}");

        // ...or change which exceptions Try catches, for the whole application. Always reset afterwards.
        try
        {
            ResultConfiguration.Configure(options => options.ShouldCatch = static _ => true);
            var caught = Result.Try(ThrowCancelled);
            await output.WriteLineAsync($"ShouldCatch lets Try catch cancellation: kind {ErrorOf(caught)?.Kind}");

            // The default mapping is a setting too. Here every exception becomes the same user-facing error with a
            // code, and the exception stays the cause for the logs.
            ResultConfiguration.Configure(options => options.ExceptionMapper = static exception =>
                Error.Unexpected("Something went wrong.").WithCode("E_UNEXPECTED").WithException(exception));
            var mapped = Result.Try(() => int.Parse("abc", System.Globalization.CultureInfo.InvariantCulture));
            await output.WriteLineAsync($"ExceptionMapper: code {ErrorOf(mapped)?.Code}, message {ErrorOf(mapped)?.Message}");
        }
        finally
        {
            ResultConfiguration.Reset();
        }

        var defaults = Result.Try(() => int.Parse("abc", System.Globalization.CultureInfo.InvariantCulture));
        await output.WriteLineAsync($"After Reset the defaults are back: kind {ErrorOf(defaults)?.Kind}");

        // The other direction: leave the railway as an exception. The exception carries the Error.
        try
        {
            Result.Failure(Error.Conflict("The order changed.")).ThrowIfFailure();
        }
        catch (ResultFailedException exception)
        {
            await output.WriteLineAsync($"ThrowIfFailure leaves the railway: ResultFailedException carrying kind {exception.Error.Kind}");
        }
    }

    // A method that returns nothing converts only to an Action, so Try runs it right here and now.
    private static void ThrowCancelled() => throw new OperationCanceledException();

    private static Error? ErrorOf(Result result) => result.TryGetError(out var error) ? error : null;

    private static Error? ErrorOf<T>(Result<T> result) => result.TryGetError(out var error) ? error : null;

    private static async ValueTask<int> ThrowTimeoutAsync()
    {
        await Task.Yield();

        throw new TimeoutException("The gateway did not answer.");
    }
}
