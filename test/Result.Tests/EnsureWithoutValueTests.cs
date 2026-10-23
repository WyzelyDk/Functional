namespace Wyzely.Functional.ResultTests;

// Ensure also exists for an operation that does not need the value (on a typed result) and for a condition that does not depend on a value
// (on an untyped result). These tests pin what a caller can write and which form a call binds to; the behavior of every variant is in the
// generated matrix tests.
public sealed class EnsureWithoutValueTests
{
    private static readonly Error Rejected = Error.Create("Rejected.");

    [Fact]
    internal void Ensure_keeps_the_value_when_an_operation_without_a_parameter_succeeds()
    {
        var outcome = Result.Success(2).Ensure(static () => Result.Success());

        Assert.Equal(2, outcome.GetValue());
    }

    [Fact]
    internal void Ensure_returns_the_error_when_an_operation_without_a_parameter_fails()
    {
        var outcome = Result.Success(2).Ensure(static () => Result.Failure(Rejected));

        Assert.Same(Rejected, outcome.GetError());
    }

    [Fact]
    internal void Ensure_keeps_the_value_when_an_operation_without_a_parameter_returns_a_typed_result_that_succeeds()
    {
        var outcome = Result.Success(2).Ensure(static () => Result.Success("checked"));

        Assert.Equal(2, outcome.GetValue());
    }

    [Fact]
    internal void Ensure_accepts_a_method_group_without_a_parameter_that_returns_an_untyped_result()
    {
        var outcome = Result.Success(2).Ensure(Validate);

        Assert.Equal(2, outcome.GetValue());
    }

    [Fact]
    internal void Ensure_accepts_a_method_group_without_a_parameter_that_returns_a_typed_result()
    {
        var outcome = Result.Success(2).Ensure(Describe);

        Assert.Equal(2, outcome.GetValue());
    }

    [Fact]
    internal async Task Ensure_awaits_an_async_lambda_without_a_parameter()
    {
        var task = Result.Success(2).Ensure(static async () =>
        {
            await Task.Yield();

            return Result.Failure(Rejected);
        });

        var outcome = await task;

        Assert.Same(Rejected, outcome.GetError());
    }

    [Fact]
    internal void Ensure_binds_a_lambda_with_a_parameter_to_the_form_that_receives_the_value()
    {
        var seen = 0;

        var outcome = Result.Success(2).Ensure(value =>
        {
            seen = value;

            return Result.Success();
        });

        Assert.Multiple(
            () => Assert.Equal(2, outcome.GetValue()),
            () => Assert.Equal(2, seen));
    }

    [Fact]
    internal void Ensure_on_an_untyped_result_keeps_the_success_when_the_condition_holds()
    {
        var outcome = Result.Success().Ensure(static () => true, Rejected);

        Assert.True(outcome.IsSuccess);
    }

    [Fact]
    internal void Ensure_on_an_untyped_result_returns_the_error_when_the_condition_does_not_hold()
    {
        var outcome = Result.Success().Ensure(static () => false, Rejected);

        Assert.Same(Rejected, outcome.GetError());
    }

    [Fact]
    internal void Ensure_on_an_untyped_result_creates_the_error_from_the_message_when_the_condition_does_not_hold()
    {
        var outcome = Result.Success().Ensure(static () => false, "Nope.");

        Assert.Equal("Nope.", outcome.GetError().Message);
    }

    [Fact]
    internal async Task Ensure_on_an_untyped_result_awaits_an_async_condition()
    {
        var task = Result.Success().Ensure(
            static async () =>
            {
                await Task.Yield();

                return false;
            },
            Rejected);

        var outcome = await task;

        Assert.Same(Rejected, outcome.GetError());
    }

    [Fact]
    internal async Task Ensure_on_an_untyped_result_awaits_a_value_task_condition()
    {
        var task = Result.Success().Ensure(static () => new ValueTask<bool>(true), Rejected);

        var outcome = await task;

        Assert.True(outcome.IsSuccess);
    }

    [Fact]
    internal async Task Ensure_on_an_untyped_task_receiver_does_not_evaluate_the_condition_when_the_receiver_failed()
    {
        var receiver = Task.FromResult(Result.Failure(Rejected));
        var evaluated = false;

        var outcome = await receiver.Ensure(
            () =>
            {
                evaluated = true;

                return true;
            },
            Error.Create("Other."));

        Assert.Multiple(
            () => Assert.Same(Rejected, outcome.GetError()),
            () => Assert.False(evaluated));
    }

    private static Result Validate() => Result.Success();

    private static Result<string> Describe() => Result.Success("checked");
}
