namespace Wyzely.Functional.ResultTests;

public sealed class TryTests
{
    private static readonly InvalidOperationException Boom = new("boom");

    [Fact]
    internal void Try_returns_success_when_the_action_completes()
    {
        var ran = false;

        var result = Result.Try(() => { ran = true; });

        Assert.Multiple(
            () => Assert.True(result.IsSuccess),
            () => Assert.True(ran));
    }

    [Fact]
    internal void Try_returns_the_value_of_the_function()
    {
        var result = Result.Try(() => 42);

        Assert.Equal(42, result.GetValue());
    }

    [Fact]
    internal void Try_turns_an_exception_of_an_action_into_a_failure_with_the_exception_as_the_cause()
    {
        var result = Result.Try(ThrowBoom);

        Assert.Multiple(
            () => Assert.True(result.IsFailure),
            () => Assert.Same(Boom, result.GetError().Exception),
            () => Assert.Equal(ErrorKind.Unexpected, result.GetError().Kind));
    }

    [Fact]
    internal void Try_turns_an_exception_of_a_function_into_a_failure()
    {
        var result = Result.Try(ThrowBoomWithValue);

        Assert.Same(Boom, result.GetError().Exception);
    }

    [Fact]
    internal void Try_uses_the_given_function_to_create_the_error()
    {
        var result = Result.Try(ThrowBoom, exception => Error.Validation(exception.Message));

        Assert.Equal(ErrorKind.Validation, result.GetError().Kind);
    }

    [Fact]
    internal void Try_lets_a_cancellation_escape()
    {
        Assert.Throws<OperationCanceledException>(() => Result.Try(ThrowCancelled));
    }

    [Fact]
    internal void Try_throws_when_the_action_is_null()
    {
        Assert.Throws<ArgumentNullException>(() => Result.Try((Action)null!));
    }

    [Fact]
    internal async Task Try_awaits_a_task_and_returns_success()
    {
        var result = await Result.Try(async () => await Task.Yield());

        Assert.True(result.IsSuccess);
    }

    [Fact]
    internal async Task Try_awaits_a_task_with_a_value_and_returns_the_value()
    {
        var result = await Result.Try(async () =>
        {
            await Task.Yield();

            return 7;
        });

        Assert.Equal(7, result.GetValue());
    }

    [Fact]
    internal async Task Try_turns_an_exception_of_a_task_into_a_failure()
    {
        var result = await Result.Try(ThrowBoomAsync);

        Assert.Same(Boom, result.GetError().Exception);
    }

    [Fact]
    internal async Task Try_turns_an_exception_of_a_typed_task_into_a_failure_using_the_given_function()
    {
        var result = await Result.Try(ThrowBoomWithValueAsync, exception => Error.Conflict(exception.Message));

        Assert.Equal(ErrorKind.Conflict, result.GetError().Kind);
    }

    [Fact]
    internal async Task Try_lets_a_cancellation_of_a_task_escape()
    {
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Result.Try(ThrowCancelledAsync));
    }

    [Fact]
    internal async Task Try_awaits_a_value_task_and_returns_success()
    {
        var result = await Result.Try(() => ValueTask.CompletedTask);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    internal async Task Try_awaits_a_value_task_with_a_value_and_returns_the_value()
    {
        var result = await Result.Try(() => new ValueTask<int>(9));

        Assert.Equal(9, result.GetValue());
    }

    [Fact]
    internal async Task Try_turns_an_exception_of_a_value_task_into_a_failure()
    {
        var result = await Result.Try(() => ValueTask.FromException(Boom));

        Assert.Same(Boom, result.GetError().Exception);
    }

    [Fact]
    internal void Try_throws_when_the_function_of_a_task_is_null()
    {
        Func<Task>? nothing = null;

        Assert.Throws<ArgumentNullException>(() => { _ = Result.Try(nothing!); });
    }

    private static void ThrowBoom() => throw Boom;

    private static int ThrowBoomWithValue() => throw Boom;

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

    private static async Task ThrowCancelledAsync()
    {
        await Task.Yield();

        throw new OperationCanceledException();
    }
}
