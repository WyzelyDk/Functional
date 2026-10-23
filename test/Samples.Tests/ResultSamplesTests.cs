using Wyzely.Functional.ResultSamples;

namespace Wyzely.Functional.SamplesTests;

// Every example of samples/Result.Samples is run here, so the teaching code cannot silently stop working.
// The configuration and exception examples change the process-wide ResultConfiguration for a moment and reset it afterwards.
public sealed class ResultSamplesTests
{
    [Fact]
    internal void BasicsExample_prints_how_a_result_is_created_and_read_when_run()
    {
        var lines = RunToLines(BasicsExample.Run);

        Assert.Multiple(
            () => Assert.Contains("ok.IsSuccess: True", lines),
            () => Assert.Contains("failed.IsFailure: True", lines),
            () => Assert.Contains("TryGetValue on the success: 42", lines),
            () => Assert.Contains("TryGetError on the failure: Order 7 was not found.", lines),
            () => Assert.Contains("Match on the success: value 42", lines),
            () => Assert.Contains("Match on the failure: error Order 7 was not found.", lines),
            () => Assert.Contains("ValueOr on the failure: -1", lines),
            () => Assert.Contains("GetValue on the failure throws ResultFailedException: The result failed: Order 7 was not found.", lines),
            () => Assert.Contains("GetError on the success throws InvalidOperationException.", lines),
            () => Assert.Contains("Deconstruct of the success: value=42, error=none", lines),
            () => Assert.Contains("ToString of the failure: Failure: Order 7 was not found.", lines),
            () => Assert.Contains("ToString of an untyped success: Success", lines),
            () => Assert.Contains("A default result: ToString is Success(0) and IsSuccess is True.", lines));
    }

    [Fact]
    internal void ErrorsExample_prints_the_parts_of_an_error_when_run()
    {
        var lines = RunToLines(ErrorsExample.Run);

        Assert.Multiple(
            () => Assert.Contains("Kind: NotFound", lines),
            () => Assert.Contains("Template: Order {OrderId} of {Customer} was not found.", lines),
            () => Assert.Contains("Message: Order 7 of anna@example.com was not found.", lines),
            () => Assert.Contains("RedactedMessage: Order 7 of *** was not found.", lines),
            () => Assert.Contains("Detail: OrderId=7", lines),
            () => Assert.Contains("Detail: Customer=***", lines),
            () => Assert.Contains("Custom kind: RateLimited", lines),
            () => Assert.Contains("With two context frames: Web: Checkout: Order 7 of anna@example.com was not found.", lines),
            () => Assert.Contains("The original error is unchanged: Order 7 of anna@example.com was not found.", lines),
            () => Assert.Contains("Aggregate message: Name is missing., Email is invalid.", lines),
            () => Assert.Contains("Aggregate kind when every child has the same kind: Validation", lines),
            () => Assert.Contains("Aggregate children: 2", lines),
            () => Assert.Contains("Aggregate with context, first child: Signup: Name is missing.", lines),
            () => Assert.Contains("Aggregate kind when the kinds differ: Failure", lines),
            () => Assert.Contains("Unavailable is transient: True", lines),
            () => Assert.Contains("Validation is transient: False", lines),
            () => Assert.Contains("FromException(TimeoutException): kind Timeout, transient True", lines),
            () => Assert.Contains("FromException keeps the cause: TimeoutException", lines),
            () => Assert.Contains("FromException escapes braces, template: Bad {{json}}", lines),
            () => Assert.Contains("FromException message: Bad {json}", lines),
            () => Assert.Contains("A failed result prints the redacted text: Failure: Order 7 of *** was not found.", lines),
            () => Assert.Contains("Equal by structure: True", lines));
    }

    [Fact]
    internal void RailwayExample_prints_what_each_operation_does_when_run()
    {
        var lines = RunToLines(RailwayExample.Run);

        Assert.Multiple(
            () => Assert.Contains("Checkout 7: receipt R-7 for 500 cents", lines),
            () => Assert.Contains("Checkout 8: Checkout: The order is closed.", lines),
            () => Assert.Contains("Checkout 99: Checkout: Order 99 was not found.", lines),
            () => Assert.Contains("Checkout 11: Checkout: Not enough COFFEE in stock (wanted 3, have 1).", lines),
            () => Assert.Contains("Tap: logged receipt R-7", lines),
            () => Assert.Contains("TapError: logged failure Order 99 was not found.", lines),
            () => Assert.Contains("Map: Anna", lines),
            () => Assert.Contains("MapError: Unknown customer. (Validation)", lines),
            () => Assert.Contains("Recover: Guest", lines),
            () => Assert.Contains("Compensate order 12: I-12", lines),
            () => Assert.Contains("Compensate order 9: Customer 2 is blocked.", lines),
            () => Assert.Contains("Ensure with a message: The order has no lines.", lines),
            () => Assert.Contains("Ensure with an operation keeps the order: 7", lines),
            () => Assert.Contains("Bind to an untyped result: Success", lines),
            () => Assert.Contains("Switch: failure Order 99 was not found.", lines),
            () => Assert.Contains("Flatten: Success(5)", lines));
    }

    [Fact]
    internal void AsyncExample_prints_every_combination_of_receiver_and_delegate_when_run()
    {
        var lines = RunToLines(AsyncExample.Run);

        Assert.Multiple(
            () => Assert.Contains("Chain, order 7: R-7", lines),
            () => Assert.Contains("Chain, order 8: Checkout: The order is closed.", lines),
            () => Assert.Contains("Charges made for the closed order: 0", lines),
            () => Assert.Contains("Result receiver, sync delegate: Success(2)", lines),
            () => Assert.Contains("Result receiver, Task delegate: Success(2)", lines),
            () => Assert.Contains("Result receiver, ValueTask delegate: Success(2)", lines),
            () => Assert.Contains("Task receiver, sync delegate: Success(2)", lines),
            () => Assert.Contains("Task receiver, Task delegate: Success(2)", lines),
            () => Assert.Contains("Task receiver, ValueTask delegate: Success(2)", lines),
            () => Assert.Contains("ValueTask receiver, sync delegate: Success(2)", lines),
            () => Assert.Contains("ValueTask receiver, Task delegate: Success(2)", lines),
            () => Assert.Contains("ValueTask receiver, ValueTask delegate: Success(2)", lines),
            () => Assert.Contains("Async lambda: R-7 (checked)", lines),
            () => Assert.Contains("A null delegate throws at the call, before any await: ArgumentNullException", lines));
    }

    [Fact]
    internal void CombiningExample_prints_how_results_are_combined_when_run()
    {
        var lines = RunToLines(CombiningExample.Run);

        Assert.Multiple(
            () => Assert.Contains("Valid signup: Success", lines),
            () => Assert.Contains("Invalid signup: Name is missing., Email is invalid.", lines),
            () => Assert.Contains("Invalid signup problems: 2", lines),
            () => Assert.Contains("Typed combine values: 1, 2, 3", lines),
            () => Assert.Contains("Typed combine with a failure: Order 98 was not found.", lines),
            () => Assert.Contains("Zip: Anna has order 7", lines),
            () => Assert.Contains("Zip, both fail: Customer 99 was not found., Order 98 was not found.", lines),
            () => Assert.Contains("Zip with a selector: Anna/7", lines),
            () => Assert.Contains("Zip of three: Anna, order 7, 500 cents", lines),
            () => Assert.Contains("Zip of three, two fail: Customer 99 was not found., Order 98 was not found.", lines),
            () => Assert.Contains("Partition: 2 values (7, 8), 2 errors (Order 98 was not found.; Order 99 was not found.)", lines),
            () => Assert.Contains("LINQ: Anna placed order 7", lines),
            () => Assert.Contains("LINQ stops at the first failure: Customer 99 was not found.", lines));
    }

    [Fact]
    internal void ExceptionsExample_prints_how_exceptions_become_results_when_run()
    {
        var lines = RunToLines(ExceptionsExample.Run);

        Assert.Multiple(
            () => Assert.Contains("Try, success: 42", lines),
            () => Assert.Contains("Try, failure: kind Unexpected, cause FormatException", lines),
            () => Assert.Contains("Try with your own mapping: 'abc' is not a valid quantity. (cause FormatException)", lines),
            () => Assert.Contains("Try for one kind of exception: 'abc' is not a number. (cause FormatException)", lines),
            () => Assert.Contains("Another kind of exception keeps travelling: FormatException", lines),
            () => Assert.Contains("A named cancellation is caught: kind Timeout", lines),
            () => Assert.Contains("Untyped Try: Success", lines),
            () => Assert.Contains("Async Try (Task): kind Unexpected, cause InvalidOperationException", lines),
            () => Assert.Contains("Async Try (ValueTask): kind Timeout, transient True", lines),
            () => Assert.Contains("Cancellation is not caught by default: OperationCanceledException", lines),
            () => Assert.Contains("Error.Cancelled(): The operation was cancelled. (kind Cancelled)", lines),
            () => Assert.Contains("ShouldCatch lets Try catch cancellation: kind Cancelled", lines),
            () => Assert.Contains("ExceptionMapper: code E_UNEXPECTED, message Something went wrong.", lines),
            () => Assert.Contains("FromException: kind Timeout, transient True", lines),
            () => Assert.Contains("ThrowIfFailure leaves the railway: ResultFailedException carrying kind Conflict", lines),
            () => Assert.Contains("After Reset the defaults are back: kind Unexpected", lines));
    }

    [Fact]
    internal void ParallelExample_prints_how_items_are_processed_in_parallel_when_run()
    {
        var lines = RunToLines(ParallelExample.Run);

        Assert.Multiple(
            () => Assert.Contains("Values in source order: 7, 8, 12", lines),
            () => Assert.Contains("Errors in source order: Order 98 was not found., Order 99 was not found.", lines),
            () => Assert.Contains("Lookups attempted: 4", lines),
            () => Assert.Contains("Never more than 2 at the same time: True", lines),
            () => Assert.Contains("ForEachInParallel (ValueTask): Success", lines),
            () => Assert.Contains("ForEachInParallel with a failure: Not enough COFFEE in stock (wanted 3, have 1).", lines),
            () => Assert.Contains("Cancellation surfaces as an exception: True", lines),
            () => Assert.Contains("An exception in the operation escapes: InvalidOperationException", lines),
            () => Assert.Contains("Wrapped in Result.Try it becomes a failure: kind Unexpected", lines),
            () => Assert.Contains("Combine of tasks: Order 98 was not found.", lines));
    }

    [Fact]
    internal void ConfigurationExample_prints_the_effect_of_each_setting_when_run()
    {
        var lines = RunToLines(ConfigurationExample.Run);

        Assert.Multiple(
            () => Assert.Contains("Defaults: Name is missing., Email is invalid.", lines),
            () => Assert.Contains("ChildSeparator ' | ': Name is missing. | Email is invalid.", lines),
            () => Assert.Contains("ContextSeparator ' > ': Web > Checkout > Order 7 was not found.", lines),
            () => Assert.Contains("RedactedValue '[hidden]': Order 7 of [hidden] was not found.", lines),
            () => Assert.Contains("AggregateMessage: Several problems occurred.", lines),
            () => Assert.Contains("CancelledMessage: Cancelled by the user.", lines),
            () => Assert.Contains("An error rendered before the change keeps its text: Name is missing., Email is invalid.", lines),
            () => Assert.Contains("A kept options object changes nothing: Name is missing. | Email is invalid.", lines),
            () => Assert.Contains("DefaultMaxDegreeOfParallelism 2, never more than 2 at the same time: True", lines),
            () => Assert.Contains("After Reset: Name is missing., Email is invalid.", lines));
    }

    [Fact]
    internal void UntypedExample_prints_how_a_result_without_a_value_is_used_when_run()
    {
        var lines = RunToLines(UntypedExample.Run);

        Assert.Multiple(
            () => Assert.Contains("Implicit conversion drops the value: Success", lines),
            () => Assert.Contains("ToResult drops the value: Success", lines),
            () => Assert.Contains("Reserve then charge: R-7", lines),
            () => Assert.Contains("Reserve fails, charge never runs: Not enough COFFEE in stock (wanted 3, have 1). (charges: 0)", lines),
            () => Assert.Contains("Map from an untyped result: 42", lines),
            () => Assert.Contains("Match on an untyped result: reserved", lines),
            () => Assert.Contains("Compensate on an untyped result: Success", lines),
            () => Assert.Contains("SuccessIf(false): Failure: The order is closed.", lines),
            () => Assert.Contains("FailureIf(false): Success", lines),
            () => Assert.Contains("Untyped combine: Name is missing.", lines));
    }

    [Fact]
    internal void UnionsExample_prints_the_union_switch_or_says_why_it_is_skipped_when_run()
    {
        var lines = RunToLines(UnionsExample.Run);

#if NET11_0_OR_GREATER
        Assert.Multiple(
            () => Assert.Contains("Describe a success: ok: 42", lines),
            () => Assert.Contains("Describe a failure: failed: Order 7 was not found.", lines),
            () => Assert.Contains("A value converts to a result: ok: 5", lines),
            () => Assert.Contains("An error converts to a result: failed: Closed.", lines));
#else
        Assert.Contains("C# 15 unions need the net11.0 target; this build does not have it, so the example is skipped.", lines);
#endif
    }

    private static string[] RunToLines(Action<TextWriter> run)
    {
        using var writer = new StringWriter();

        run(writer);

        return writer.ToString().Split(["\r\n", "\n"], StringSplitOptions.RemoveEmptyEntries);
    }
}
