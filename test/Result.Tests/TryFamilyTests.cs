namespace Wyzely.Functional.ResultTests;

// MapTry, BindTry, TapTry and TapIfTry run a delegate that may throw and turn the exception into a failure, like Result.Try. The matrix tests cover
// every combination; these pin what a caller can write and what keeps travelling.
public sealed class TryFamilyTests
{
    private static readonly Error Handled = Error.Create("Handled.");
    private static readonly Func<int, int> Throw = static _ => Boom.Throw<int>();
    private static readonly Func<int, int> ThrowFormat = static _ => throw new FormatException("Format.");
    private static readonly Func<int, int> Cancel = static _ => throw new OperationCanceledException();
    private static readonly Func<Exception, Error> HandleAny = static _ => Handled;

    [Fact]
    internal void MapTry_turns_a_thrown_exception_into_a_failure_that_keeps_the_exception_as_the_cause()
    {
        var outcome = Result.Success(2).MapTry(Throw);

        Assert.Multiple(
            () => Assert.True(outcome.IsFailure),
            () => Assert.IsType<InvalidOperationException>(outcome.GetError().Exception));
    }

    [Fact]
    internal void MapTry_uses_the_handler_to_create_the_error()
    {
        var outcome = Result.Success(2).MapTry(Throw, static _ => Handled);

        Assert.Same(Handled, outcome.GetError());
    }

    [Fact]
    internal void MapTry_lets_a_cancellation_escape_by_default()
    {
        var result = Result.Success(2);

        Assert.Throws<OperationCanceledException>(() => result.MapTry(Cancel));
    }

    [Fact]
    internal void MapTry_with_a_named_exception_catches_only_that_type()
    {
        var result = Result.Success(2);

        var caught = result.MapTry(ThrowFormat, static (FormatException exception) => Handled);

        Assert.Multiple(
            () => Assert.Same(Handled, caught.GetError()),
            () => Assert.Throws<InvalidOperationException>(() => result.MapTry(Throw, static (FormatException exception) => Handled)));
    }

    [Fact]
    internal void MapTry_with_a_named_exception_catches_a_type_derived_from_it()
    {
        var outcome = Result.Success(2).MapTry(ThrowFormat, static (Exception exception) => Handled, static _ => true);

        Assert.Same(Handled, outcome.GetError());
    }

    [Fact]
    internal void MapTry_with_a_condition_catches_the_exception_only_when_the_condition_holds()
    {
        var result = Result.Success(2);

        var caught = result.MapTry(Throw, static (InvalidOperationException exception) => Handled, static exception => exception.Message == "Boom.");

        Assert.Multiple(
            () => Assert.Same(Handled, caught.GetError()),
            () => Assert.Throws<InvalidOperationException>(
                () => result.MapTry(Throw, static (InvalidOperationException exception) => Handled, static _ => false)));
    }

    [Fact]
    internal void MapTry_with_a_handler_for_a_plain_exception_follows_the_setting_so_a_cancellation_escapes()
    {
        var result = Result.Success(2);

        Assert.Throws<OperationCanceledException>(() => result.MapTry(Cancel, HandleAny));
    }

    [Fact]
    internal void MapTry_throws_when_the_handler_returns_null()
    {
        var result = Result.Success(2);

        var exception = Record.Exception(() => result.MapTry(Throw, NoError));

        Assert.IsType<ArgumentNullException>(exception);
    }

    [Fact]
    internal void MapTry_throws_when_the_delegate_is_null()
    {
        var result = Result.Success(2);

        var exception = Record.Exception(() => result.MapTry((Func<int, int>)null!));

        Assert.Equal("map", Assert.IsType<ArgumentNullException>(exception).ParamName);
    }

    [Fact]
    internal void MapTry_throws_when_the_named_handler_is_null()
    {
        var result = Result.Success(2);

        var exception = Record.Exception(() => result.MapTry(Throw, (Func<FormatException, Error>)null!));

        Assert.Equal("onException", Assert.IsType<ArgumentNullException>(exception).ParamName);
    }

    [Fact]
    internal void BindTry_returns_the_result_of_the_delegate_when_it_does_not_throw()
    {
        var outcome = Result.Success(2).BindTry(static value => Result.Success(value * 10));

        Assert.Equal(20, outcome.GetValue());
    }

    [Fact]
    internal void BindTry_on_an_untyped_result_turns_a_thrown_exception_into_a_failure()
    {
        var outcome = Result.Success().BindTry(static () => Boom.Throw<Result>(), static _ => Handled);

        Assert.Same(Handled, outcome.GetError());
    }

    [Fact]
    internal void TapTry_returns_the_receiver_when_the_delegate_does_not_throw()
    {
        var calls = 0;

        var outcome = Result.Success(2).TapTry(_ => calls++);

        Assert.Multiple(
            () => Assert.Equal(2, outcome.GetValue()),
            () => Assert.Equal(1, calls));
    }

    [Fact]
    internal void TapIfTry_does_not_run_the_delegate_when_the_condition_does_not_hold()
    {
        var calls = 0;

        var outcome = Result.Success(2).TapIfTry(static value => value > 5, _ => calls++);

        Assert.Multiple(
            () => Assert.Equal(2, outcome.GetValue()),
            () => Assert.Equal(0, calls));
    }

    [Fact]
    internal async Task MapTry_turns_an_exception_thrown_by_an_async_delegate_into_a_failure()
    {
        var task = AsTask(Result.Success(2).MapTry(static async value =>
        {
            await Task.Yield();

            return Boom.Throw<int>() + value;
        }));

        var outcome = await task;

        Assert.IsType<InvalidOperationException>(outcome.GetError().Exception);
    }

    [Fact]
    internal async Task MapTry_with_a_value_task_delegate_returns_a_value_task()
    {
        var task = AsValueTask(Result.Success(2).MapTry(static value => new ValueTask<int>(value * 10)));

        var outcome = await task;

        Assert.Equal(20, outcome.GetValue());
    }

    [Fact]
    internal async Task MapTry_lets_a_cancellation_of_an_async_delegate_escape()
    {
        var receiver = Task.FromResult(Result.Success(2));

        await Assert.ThrowsAsync<OperationCanceledException>(() => receiver.MapTry(static async _ =>
        {
            await Task.Yield();

            return Cancel(0);
        }));
    }

    [Fact]
    internal void TapTry_returns_the_receiver_task_when_the_delegate_does_not_throw()
    {
        var receiver = Task.FromResult(Result.Success(2));

        var returned = receiver.TapTry(static _ => { });

        Assert.Same(receiver, returned);
    }

    [Fact]
    internal void TapIfTry_returns_the_receiver_task_when_a_plain_condition_is_false()
    {
        var receiver = Task.FromResult(Result.Success(2));

        var returned = receiver.TapIfTry(false, static _ => { });

        Assert.Same(receiver, returned);
    }

    // Passing the call through a parameter of an exact type pins the type the overload returns.
    private static Task<Result<int>> AsTask(Task<Result<int>> task) => task;

    private static ValueTask<Result<int>> AsValueTask(ValueTask<Result<int>> task) => task;

    private static Error NoError(Exception exception) => exception is null ? Handled : null!;
}
