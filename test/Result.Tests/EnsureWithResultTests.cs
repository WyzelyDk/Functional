namespace Wyzely.Functional.ResultTests;

// Ensure with an operation that returns a result exists for an untyped result and for a typed one. A lambda binds to either, but a method
// group can only bind to the form whose return type it matches exactly, so the tests pin what a caller can write.
public sealed class EnsureWithResultTests
{
    private static readonly Error Rejected = Error.Create("Rejected.");

    [Fact]
    internal void Ensure_keeps_the_original_value_when_a_validation_returns_a_typed_result_that_succeeds()
    {
        var outcome = Result.Success(2).Ensure(value => Result.Success(value.ToString(System.Globalization.CultureInfo.InvariantCulture)));

        Assert.Equal(2, outcome.GetValue());
    }

    [Fact]
    internal void Ensure_returns_the_error_of_the_validation_when_a_validation_returns_a_typed_result_that_fails()
    {
        var outcome = Result.Success(2).Ensure(static _ => Result.Failure<string>(Rejected));

        Assert.Same(Rejected, outcome.GetError());
    }

    [Fact]
    internal void Ensure_accepts_a_method_group_that_returns_a_typed_result()
    {
        var outcome = Result.Success(2).Ensure(Describe);

        Assert.Equal(2, outcome.GetValue());
    }

    [Fact]
    internal void Ensure_accepts_a_method_group_that_returns_an_untyped_result()
    {
        var outcome = Result.Success(2).Ensure(Validate);

        Assert.Equal(2, outcome.GetValue());
    }

    [Fact]
    internal async Task Ensure_awaits_an_async_lambda_that_returns_a_typed_result()
    {
        var task = Result.Success(2).Ensure(static async value =>
        {
            await Task.Yield();

            return Result.Success(value + 1);
        });

        var outcome = await task;

        Assert.Equal(2, outcome.GetValue());
    }

    [Fact]
    internal async Task Ensure_awaits_a_value_task_that_returns_a_typed_result()
    {
        var task = Result.Success(2).Ensure(static value => new ValueTask<Result<int>>(Result.Failure<int>(Rejected)));

        var outcome = await task;

        Assert.Same(Rejected, outcome.GetError());
    }

    [Fact]
    internal async Task Ensure_with_a_typed_validation_accepts_a_task_receiver()
    {
        var receiver = Task.FromResult(Result.Success(2));

        var outcome = await receiver.Ensure(static _ => Result.Success("checked"));

        Assert.Equal(2, outcome.GetValue());
    }

    [Fact]
    internal void Ensure_does_not_call_a_typed_validation_when_the_receiver_failed()
    {
        var calls = 0;

        var outcome = Result.Failure<int>(Rejected).Ensure(_ =>
        {
            calls++;

            return Result.Success("checked");
        });

        Assert.Multiple(
            () => Assert.Same(Rejected, outcome.GetError()),
            () => Assert.Equal(0, calls));
    }

    [Fact]
    internal void Ensure_with_a_predicate_and_an_error_still_binds()
    {
        var outcome = Result.Success(2).Ensure(static value => value > 5, Rejected);

        Assert.Same(Rejected, outcome.GetError());
    }

    private static Result<string> Describe(int value) => Result.Success(value.ToString(System.Globalization.CultureInfo.InvariantCulture));

    private static Result Validate(int value) => Result.SuccessIf(value > 0, Rejected);
}
