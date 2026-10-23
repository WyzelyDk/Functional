namespace Wyzely.Functional.ResultTests;

// Tap without a value, EnsureNot, EnsureNotNull and SuccessIfNotNull. The matrix tests cover every combination; these pin what a caller can write,
// which overload a call binds to, and the guards.
public sealed class ConveniencesTests
{
    private static readonly Error Missing = Error.Create("Missing.");

    [Fact]
    internal void Tap_without_a_value_runs_the_side_effect_for_a_successful_result()
    {
        var calls = 0;

        var outcome = Result.Success(2).Tap(() => calls++);

        Assert.Multiple(
            () => Assert.Equal(2, outcome.GetValue()),
            () => Assert.Equal(1, calls));
    }

    [Fact]
    internal void Tap_binds_a_lambda_without_parameters_and_a_lambda_with_the_value_to_different_overloads()
    {
        var withoutValue = 0;
        var withValue = 0;

        Result.Success(2).Tap(() => withoutValue++).Tap(value => withValue += value);

        Assert.Multiple(
            () => Assert.Equal(1, withoutValue),
            () => Assert.Equal(2, withValue));
    }

    [Fact]
    internal async Task Tap_without_a_value_awaits_an_async_lambda_and_returns_a_task()
    {
        var task = AsTask(Result.Success(2).Tap(static async () => await Task.Yield()));

        var outcome = await task;

        Assert.Equal(2, outcome.GetValue());
    }

    [Fact]
    internal void EnsureNot_fails_when_the_predicate_holds()
    {
        var outcome = Result.Success(2).EnsureNot(static value => value > 1, Missing);

        Assert.Same(Missing, outcome.GetError());
    }

    [Fact]
    internal void EnsureNot_keeps_the_value_when_the_predicate_does_not_hold()
    {
        var outcome = Result.Success(2).EnsureNot(static value => value > 5, "Too big.");

        Assert.Equal(2, outcome.GetValue());
    }

    [Fact]
    internal async Task EnsureNot_accepts_an_async_predicate()
    {
        var outcome = await Result.Success(2).EnsureNot(static value => Task.FromResult(value > 1), "Too big.");

        Assert.Equal("Too big.", outcome.GetError().Message);
    }

    [Fact]
    internal void EnsureNotNull_turns_a_nullable_reference_into_a_non_nullable_one()
    {
        var outcome = Result.Success<string?>("abc").EnsureNotNull(Missing);

        Assert.Equal(3, outcome.GetValue().Length);
    }

    [Fact]
    internal void EnsureNotNull_unwraps_a_nullable_value_type()
    {
        var outcome = Result.Success<int?>(5).EnsureNotNull(Missing);

        Assert.Equal(5, outcome.GetValue());
    }

    [Fact]
    internal void EnsureNotNull_fails_for_a_null_value_of_either_kind()
    {
        Assert.Multiple(
            () => Assert.Same(Missing, Result.Success<string?>(null).EnsureNotNull(Missing).GetError()),
            () => Assert.Same(Missing, Result.Success<int?>(null).EnsureNotNull(Missing).GetError()));
    }

    [Fact]
    internal void EnsureNotNull_creates_the_error_only_when_the_value_is_null()
    {
        var calls = 0;

        var present = Result.Success<string?>("abc").EnsureNotNull(() =>
        {
            calls++;

            return Missing;
        });
        var absent = Result.Success<string?>(null).EnsureNotNull(() =>
        {
            calls++;

            return Missing;
        });

        Assert.Multiple(
            () => Assert.True(present.IsSuccess),
            () => Assert.Same(Missing, absent.GetError()),
            () => Assert.Equal(1, calls));
    }

    [Fact]
    internal async Task EnsureNotNull_accepts_a_task_receiver()
    {
        var outcome = await Task.FromResult(Result.Success<int?>(5)).EnsureNotNull("Missing.");

        Assert.Equal(5, outcome.GetValue());
    }

    [Fact]
    internal void EnsureNotNull_throws_when_the_error_is_null()
    {
        var result = Result.Success<string?>("abc");

        var exception = Record.Exception(() => result.EnsureNotNull((Error)null!));

        Assert.Equal("error", Assert.IsType<ArgumentNullException>(exception).ParamName);
    }

    [Fact]
    internal void SuccessIfNotNull_holds_a_reference_that_is_not_null()
    {
        var text = (string?)"abc";

        var outcome = Result.SuccessIfNotNull(text, Missing);

        Assert.Equal("abc", outcome.GetValue());
    }

    [Fact]
    internal void SuccessIfNotNull_holds_a_nullable_value_type_that_has_a_value()
    {
        var number = (int?)5;

        var outcome = Result.SuccessIfNotNull(number, Missing);

        Assert.Equal(5, outcome.GetValue());
    }

    [Fact]
    internal void SuccessIfNotNull_fails_for_a_null_of_either_kind()
    {
        var text = (string?)null;
        var number = (int?)null;

        Assert.Multiple(
            () => Assert.Same(Missing, Result.SuccessIfNotNull(text, Missing).GetError()),
            () => Assert.Equal("Absent.", Result.SuccessIfNotNull(number, "Absent.").GetError().Message));
    }

    [Fact]
    internal async Task SuccessIfNotNull_binds_a_task_of_a_nullable_reference_to_the_task_overload()
    {
        var task = Task.FromResult<string?>("abc");

        var outcome = await Result.SuccessIfNotNull(task, Missing);

        Assert.Equal("abc", outcome.GetValue());
    }

    [Fact]
    internal async Task SuccessIfNotNull_binds_a_value_task_of_a_nullable_value_type_to_the_value_task_overload()
    {
        var task = AsValueTask(Result.SuccessIfNotNull(new ValueTask<int?>(5), Missing));

        var outcome = await task;

        Assert.Equal(5, outcome.GetValue());
    }

    [Fact]
    internal void SuccessIfNotNull_does_not_create_the_error_when_there_is_a_value()
    {
        var calls = 0;

        var outcome = Result.SuccessIfNotNull("abc", () =>
        {
            calls++;

            return Missing;
        });

        Assert.Multiple(
            () => Assert.True(outcome.IsSuccess),
            () => Assert.Equal(0, calls));
    }

    [Fact]
    internal void SuccessIfNotNull_throws_when_the_message_is_empty_and_the_value_is_null()
    {
        var text = (string?)null;

        var exception = Record.Exception(() => Result.SuccessIfNotNull(text, string.Empty));

        Assert.IsType<ArgumentException>(exception);
    }

    [Fact]
    internal void Tap_without_a_value_returns_the_receiver_task_for_a_completed_receiver()
    {
        var receiver = Task.FromResult(Result.Success(2));

        var returned = receiver.Tap(static () => { });

        Assert.Same(receiver, returned);
    }

    [Fact]
    internal void EnsureNot_returns_the_receiver_task_when_the_predicate_does_not_hold()
    {
        var receiver = Task.FromResult(Result.Success(2));

        var returned = receiver.EnsureNot(static value => value > 5, Missing);

        Assert.Same(receiver, returned);
    }

    // Passing the call through a parameter of an exact type pins the type the overload returns.
    private static Task<Result<int>> AsTask(Task<Result<int>> task) => task;

    private static ValueTask<Result<int>> AsValueTask(ValueTask<Result<int>> task) => task;
}
