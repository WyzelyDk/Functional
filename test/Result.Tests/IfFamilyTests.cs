namespace Wyzely.Functional.ResultTests;

// The *If operations run their delegate only when a condition holds. The matrix tests cover every combination; these pin what a caller can
// write: a bool, a function without parameters, a function of the value, method groups and async lambdas, and that a false condition hands
// back the receiver (the very task, for a task receiver).
public sealed class IfFamilyTests
{
    private static readonly Error Rejected = Error.Create("Rejected.");

    [Fact]
    internal void MapIf_maps_the_value_when_a_plain_condition_holds()
    {
        var outcome = Result.Success(2).MapIf(true, static value => value * 10);

        Assert.Equal(20, outcome.GetValue());
    }

    [Fact]
    internal void MapIf_binds_a_function_without_parameters_as_the_condition()
    {
        var outcome = Result.Success(2).MapIf(static () => true, static value => value * 10);

        Assert.Equal(20, outcome.GetValue());
    }

    [Fact]
    internal void MapIf_binds_a_function_of_the_value_as_the_condition()
    {
        var outcome = Result.Success(2).MapIf(static value => value > 5, static value => value * 10);

        Assert.Equal(2, outcome.GetValue());
    }

    [Fact]
    internal void MapIf_accepts_method_groups_for_the_condition_and_the_delegate()
    {
        var outcome = Result.Success(2).MapIf(IsSmall, TimesTen);

        Assert.Equal(20, outcome.GetValue());
    }

    [Fact]
    internal async Task MapIf_awaits_an_async_lambda_and_returns_a_task()
    {
        var task = AsTask(Result.Success(2).MapIf(
            static value => value > 1,
            static async value =>
            {
                await Task.Yield();

                return value * 10;
            }));

        var outcome = await task;

        Assert.Equal(20, outcome.GetValue());
    }

    [Fact]
    internal async Task MapIf_with_a_value_task_delegate_returns_a_value_task()
    {
        var task = AsValueTask(Result.Success(2).MapIf(true, static value => new ValueTask<int>(value * 10)));

        var outcome = await task;

        Assert.Equal(20, outcome.GetValue());
    }

    [Fact]
    internal async Task MapIf_with_a_false_condition_returns_the_receiver_in_a_task_for_an_async_delegate()
    {
        var outcome = await Result.Success(2).MapIf(false, static value => Task.FromResult(value * 10));

        Assert.Equal(2, outcome.GetValue());
    }

    [Fact]
    internal void BindIf_keeps_the_value_when_the_condition_does_not_hold()
    {
        var outcome = Result.Success(2).BindIf(static value => value > 5, static _ => Result.Failure<int>(Rejected));

        Assert.Equal(2, outcome.GetValue());
    }

    [Fact]
    internal void BindIf_returns_the_result_of_the_delegate_when_the_condition_holds()
    {
        var outcome = Result.Success(2).BindIf(true, static _ => Result.Failure<int>(Rejected));

        Assert.Same(Rejected, outcome.GetError());
    }

    [Fact]
    internal void EnsureIf_fails_with_the_error_of_a_validation_that_returns_a_typed_result()
    {
        var outcome = Result.Success(2).EnsureIf(static () => true, static _ => Result.Failure<string>(Rejected));

        Assert.Same(Rejected, outcome.GetError());
    }

    [Fact]
    internal void EnsureIf_fails_with_the_error_of_a_validation_that_returns_an_untyped_result()
    {
        var outcome = Result.Success(2).EnsureIf(static value => value > 1, static _ => Result.Failure(Rejected));

        Assert.Same(Rejected, outcome.GetError());
    }

    [Fact]
    internal void EnsureIf_keeps_the_value_when_the_condition_does_not_hold()
    {
        var outcome = Result.Success(2).EnsureIf(false, static _ => Result.Failure(Rejected));

        Assert.Equal(2, outcome.GetValue());
    }

    [Fact]
    internal void TapIf_on_an_untyped_result_runs_the_side_effect_when_the_condition_holds()
    {
        var calls = 0;

        var outcome = Result.Success().TapIf(static () => true, () => calls++);

        Assert.Multiple(
            () => Assert.True(outcome.IsSuccess),
            () => Assert.Equal(1, calls));
    }

    [Fact]
    internal void TapErrorIf_passes_the_error_to_a_condition_that_decides()
    {
        var calls = 0;

        var outcome = Result.Failure(Rejected).TapErrorIf(static error => error.Message == "Rejected.", _ => calls++);

        Assert.Multiple(
            () => Assert.Same(Rejected, outcome.GetError()),
            () => Assert.Equal(1, calls));
    }

    [Fact]
    internal void MapIf_returns_the_receiver_task_when_a_plain_condition_is_false()
    {
        var receiver = Task.FromResult(Result.Success(2));

        var returned = receiver.MapIf(false, static value => value * 10);

        Assert.Same(receiver, returned);
    }

    [Fact]
    internal void BindIf_returns_the_receiver_task_when_a_plain_condition_is_false()
    {
        var receiver = Task.FromResult(Result.Success(2));

        var returned = receiver.BindIf(false, static value => Result.Success(value * 10));

        Assert.Same(receiver, returned);
    }

    [Fact]
    internal void TapIf_returns_the_receiver_task_whatever_the_condition()
    {
        var receiver = Task.FromResult(Result.Success(2));

        Assert.Multiple(
            () => Assert.Same(receiver, receiver.TapIf(false, static _ => { })),
            () => Assert.Same(receiver, receiver.TapIf(true, static _ => { })),
            () => Assert.Same(receiver, receiver.TapIf(static value => value > 5, static _ => { })));
    }

    [Fact]
    internal void EnsureIf_returns_the_receiver_task_when_the_condition_does_not_hold_or_the_validation_succeeds()
    {
        var receiver = Task.FromResult(Result.Success(2));

        Assert.Multiple(
            () => Assert.Same(receiver, receiver.EnsureIf(false, static _ => Result.Failure(Rejected))),
            () => Assert.Same(receiver, receiver.EnsureIf(static value => value > 1, static _ => Result.Success())));
    }

    [Fact]
    internal void TapErrorIf_returns_the_receiver_task_whatever_the_condition()
    {
        var receiver = Task.FromResult(Result.Failure<int>(Rejected));

        Assert.Multiple(
            () => Assert.Same(receiver, receiver.TapErrorIf(false, static _ => { })),
            () => Assert.Same(receiver, receiver.TapErrorIf(static () => true, static _ => { })));
    }

    [Fact]
    internal void MapIf_throws_when_the_delegate_is_null()
    {
        var result = Result.Success(2);

        var exception = Record.Exception(() => result.MapIf(true, (Func<int, int>)null!));

        Assert.Equal("map", Assert.IsType<ArgumentNullException>(exception).ParamName);
    }

    [Fact]
    internal void MapIf_throws_when_a_condition_function_is_null()
    {
        var result = Result.Success(2);

        var exception = Record.Exception(() => result.MapIf((Func<int, bool>)null!, static value => value));

        Assert.Equal("condition", Assert.IsType<ArgumentNullException>(exception).ParamName);
    }

    // Passing the call through a parameter of an exact type pins the type the overload returns.
    private static Task<Result<int>> AsTask(Task<Result<int>> task) => task;

    private static ValueTask<Result<int>> AsValueTask(ValueTask<Result<int>> task) => task;

    private static bool IsSmall(int value) => value < 5;

    private static int TimesTen(int value) => value * 10;
}
