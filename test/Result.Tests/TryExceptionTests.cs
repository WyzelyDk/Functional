using System.Globalization;

namespace Wyzely.Functional.ResultTests;

// Try with the type of the exception to catch, taken from the parameter of the handler. Naming the type is the decision, so the setting that
// decides which exceptions the plain Try catches is not consulted.
[Collection(ProcessWideConfiguration.Name)]
public sealed class TryExceptionTests : IDisposable
{
    private static readonly InvalidOperationException Boom = new("boom");

    public void Dispose() => ResultConfiguration.Reset();

    [Fact]
    internal void Try_returns_success_when_the_action_completes()
    {
        var ran = false;

        var result = Result.Try(() => ran = true, (InvalidOperationException exception) => Error.FromException(exception));

        Assert.Multiple(
            () => Assert.True(result.IsSuccess),
            () => Assert.True(ran));
    }

    [Fact]
    internal void Try_returns_success_when_an_action_without_a_value_completes()
    {
        var ran = false;

        var result = Result.Try(() => { ran = true; }, (InvalidOperationException exception) => Error.FromException(exception));

        Assert.Multiple(
            () => Assert.True(result.IsSuccess),
            () => Assert.True(ran));
    }

    [Fact]
    internal void Try_turns_an_exception_of_the_named_type_into_a_failure_that_keeps_the_cause()
    {
        var result = Result.Try(ThrowBoom, (InvalidOperationException exception) => Error.FromException(exception));

        Assert.Multiple(
            () => Assert.True(result.IsFailure),
            () => Assert.Same(Boom, result.GetError().Exception));
    }

    [Fact]
    internal void Try_uses_the_error_of_the_handler()
    {
        var result = Result.Try(ThrowBoom, (InvalidOperationException exception) => Error.Validation(exception.Message));

        Assert.Multiple(
            () => Assert.Equal(ErrorKind.Validation, result.GetError().Kind),
            () => Assert.Equal("boom", result.GetError().Message));
    }

    [Fact]
    internal void Try_lets_an_exception_of_another_type_escape()
    {
        Assert.Throws<InvalidOperationException>(() => Result.Try(ThrowBoom, (ArgumentException exception) => Error.FromException(exception)));
    }

    [Fact]
    internal void Try_catches_an_exception_of_a_type_derived_from_the_named_type()
    {
        var result = Result.Try(ThrowDisposed, (InvalidOperationException exception) => Error.FromException(exception));

        Assert.IsType<ObjectDisposedException>(result.GetError().Exception);
    }

    [Fact]
    internal void Try_catches_a_cancellation_when_its_type_is_named()
    {
        var result = Result.Try(ThrowCancelled, (OperationCanceledException exception) => Error.FromException(exception));

        Assert.True(result.IsFailure);
    }

    [Fact]
    internal void Try_does_not_consult_ShouldCatch_when_the_type_is_named()
    {
        ResultConfiguration.Configure(static options => options.ShouldCatch = static _ => false);

        var result = Result.Try(ThrowBoom, (InvalidOperationException exception) => Error.FromException(exception));

        Assert.True(result.IsFailure);
    }

    [Fact]
    internal void Try_with_a_handler_that_takes_the_plain_exception_still_follows_ShouldCatch()
    {
        Assert.Throws<OperationCanceledException>(() => Result.Try(ThrowCancelled, exception => Error.FromException(exception)));
    }

    [Fact]
    internal void Try_catches_only_when_the_condition_holds()
    {
        var caught = Result.Try(
            ThrowBoom,
            (InvalidOperationException exception) => Error.FromException(exception),
            static exception => exception.Message == "boom");

        Assert.True(caught.IsFailure);
    }

    [Fact]
    internal void Try_lets_an_exception_escape_when_the_condition_does_not_hold()
    {
        Assert.Throws<InvalidOperationException>(() => Result.Try(
            ThrowBoom,
            (InvalidOperationException exception) => Error.FromException(exception),
            static exception => exception.Message == "other"));
    }

    [Fact]
    internal void Try_throws_when_the_action_is_null()
    {
        var exception = Assert.Throws<ArgumentNullException>(() => Result.Try((Action)null!, (InvalidOperationException e) => Error.FromException(e)));

        Assert.Equal("action", exception.ParamName);
    }

    [Fact]
    internal void Try_throws_when_the_handler_is_null()
    {
        var exception = Assert.Throws<ArgumentNullException>(() => Result.Try(ThrowBoom, (Func<InvalidOperationException, Error>)null!));

        Assert.Equal("onException", exception.ParamName);
    }

    [Fact]
    internal void Try_returns_the_value_of_the_function()
    {
        var result = Result.Try(() => 42, (FormatException exception) => Error.Validation(exception.Message));

        Assert.Equal(42, result.GetValue());
    }

    [Fact]
    internal void Try_turns_an_exception_of_the_named_type_into_a_failure_for_a_function()
    {
        var result = Result.Try(() => int.Parse("x", CultureInfo.InvariantCulture), (FormatException exception) => Error.Validation(exception.Message));

        Assert.Equal(ErrorKind.Validation, result.GetError().Kind);
    }

    [Fact]
    internal void Try_lets_an_exception_of_another_type_escape_from_a_function()
    {
        Assert.Throws<FormatException>(() => Result.Try(
            () => int.Parse("x", CultureInfo.InvariantCulture),
            (InvalidOperationException exception) => Error.FromException(exception)));
    }

    [Fact]
    internal void Try_throws_when_the_function_is_null()
    {
        var exception = Assert.Throws<ArgumentNullException>(() => Result.Try((Func<int>)null!, (FormatException e) => Error.FromException(e)));

        Assert.Equal("function", exception.ParamName);
    }

    [Fact]
    internal async Task Try_awaits_a_task_and_returns_success()
    {
        var result = await Result.Try(async () => await Task.Yield(), (InvalidOperationException exception) => Error.FromException(exception));

        Assert.True(result.IsSuccess);
    }

    [Fact]
    internal async Task Try_turns_an_exception_of_the_named_type_from_a_task_into_a_failure()
    {
        var result = await Result.Try(ThrowBoomAsync, (InvalidOperationException exception) => Error.FromException(exception));

        Assert.Same(Boom, result.GetError().Exception);
    }

    [Fact]
    internal async Task Try_lets_an_exception_of_another_type_escape_from_a_task()
    {
        var task = Result.Try(ThrowBoomAsync, (ArgumentException exception) => Error.FromException(exception));

        await Assert.ThrowsAsync<InvalidOperationException>(async () => await task);
    }

    [Fact]
    internal async Task Try_returns_the_value_of_a_task_with_a_value()
    {
        var result = await Result.Try(
            static async () =>
            {
                await Task.Yield();

                return 7;
            },
            (InvalidOperationException exception) => Error.FromException(exception));

        Assert.Equal(7, result.GetValue());
    }

    [Fact]
    internal async Task Try_turns_an_exception_of_the_named_type_from_a_task_with_a_value_into_a_failure()
    {
        var result = await Result.Try(ThrowBoomWithValueAsync, (InvalidOperationException exception) => Error.FromException(exception));

        Assert.Same(Boom, result.GetError().Exception);
    }

    [Fact]
    internal async Task Try_catches_from_a_task_only_when_the_condition_holds()
    {
        var caught = await Result.Try(
            ThrowBoomAsync,
            (InvalidOperationException exception) => Error.FromException(exception),
            static exception => exception.Message == "boom");
        var task = Result.Try(
            ThrowBoomAsync,
            (InvalidOperationException exception) => Error.FromException(exception),
            static exception => exception.Message == "other");

        Assert.True(caught.IsFailure);
        await Assert.ThrowsAsync<InvalidOperationException>(async () => await task);
    }

    [Fact]
    internal async Task Try_awaits_a_value_task_and_returns_success()
    {
        var result = await Result.Try(static () => ValueTask.CompletedTask, (InvalidOperationException exception) => Error.FromException(exception));

        Assert.True(result.IsSuccess);
    }

    [Fact]
    internal async Task Try_turns_an_exception_of_the_named_type_from_a_value_task_into_a_failure()
    {
        var result = await Result.Try(ThrowBoomValueTaskAsync, (InvalidOperationException exception) => Error.FromException(exception));

        Assert.Same(Boom, result.GetError().Exception);
    }

    [Fact]
    internal async Task Try_turns_an_exception_of_the_named_type_from_a_value_task_with_a_value_into_a_failure()
    {
        var result = await Result.Try(ThrowBoomWithValueValueTaskAsync, (InvalidOperationException exception) => Error.FromException(exception));

        Assert.Same(Boom, result.GetError().Exception);
    }

    [Fact]
    internal async Task Try_returns_the_value_of_a_value_task_with_a_value()
    {
        var result = await Result.Try(static () => new ValueTask<int>(9), (InvalidOperationException exception) => Error.FromException(exception));

        Assert.Equal(9, result.GetValue());
    }

    [Fact]
    internal void Try_throws_when_a_task_function_is_null()
    {
        Func<Task>? action = null;

        var exception = Assert.Throws<ArgumentNullException>(() => Ignore(Result.Try(action!, (InvalidOperationException e) => Error.FromException(e))));

        Assert.Equal("action", exception.ParamName);
    }

    // A method that returns a task throws for a null argument before it returns, but a lambda that returns a task is, to the test framework,
    // one that throws later. Handing the task to a method that ignores it makes the lambda a plain action.
    private static void Ignore<T>(T value) => _ = value;

    private static void ThrowBoom() => throw Boom;

    private static void ThrowDisposed() => throw new ObjectDisposedException(nameof(TryExceptionTests));

    private static void ThrowCancelled() => throw new OperationCanceledException();

    private static async Task ThrowBoomAsync()
    {
        await Task.Yield();

        throw Boom;
    }

    private static async Task<int> ThrowBoomWithValueAsync()
    {
        await Task.Yield();

        throw Boom;
    }

    private static async ValueTask ThrowBoomValueTaskAsync()
    {
        await Task.Yield();

        throw Boom;
    }

    private static async ValueTask<int> ThrowBoomWithValueValueTaskAsync()
    {
        await Task.Yield();

        throw Boom;
    }
}
