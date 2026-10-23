namespace Wyzely.Functional.ResultTests;

// Ensure takes an error or a message that is created from the value, and only when the condition does not hold. The condition and the factory
// are asynchronous independently of each other. The matrix tests cover every combination; these pin what a caller can write: lambdas, async
// lambdas and method groups, and what happens when a factory misbehaves.
public sealed class EnsureErrorFactoryTests
{
    private static readonly Error Rejected = Error.Create("Rejected.");

    [Fact]
    internal void Ensure_returns_the_error_the_factory_creates_from_the_value_when_the_condition_does_not_hold()
    {
        var outcome = Result.Success(2).Ensure(static value => value > 5, static value => Error.Create("Too small.").WithDetail("value", value));

        Assert.Multiple(
            () => Assert.Equal("Too small.", outcome.GetError().Message),
            () => Assert.Equal(2, Assert.Single(outcome.GetError().Details).Value));
    }

    [Fact]
    internal void Ensure_returns_an_error_with_the_message_the_factory_creates_from_the_value_when_the_condition_does_not_hold()
    {
        var outcome = Result.Success(2).Ensure(
            static value => value > 5,
            static value => "Too small: " + value.ToString(System.Globalization.CultureInfo.InvariantCulture));

        Assert.Equal("Too small: 2", outcome.GetError().Message);
    }

    [Fact]
    internal void Ensure_does_not_call_an_error_factory_when_the_condition_holds()
    {
        var calls = 0;

        var outcome = Result.Success(2).Ensure(
            static value => value > 1,
            _ =>
            {
                calls++;

                return Rejected;
            });

        Assert.Multiple(
            () => Assert.Equal(2, outcome.GetValue()),
            () => Assert.Equal(0, calls));
    }

    [Fact]
    internal void Ensure_accepts_a_method_group_for_the_error_factory()
    {
        var outcome = Result.Success(2).Ensure(static value => value > 5, TooSmall);

        Assert.Equal("Too small.", outcome.GetError().Message);
    }

    [Fact]
    internal void Ensure_accepts_a_method_group_for_the_message_factory()
    {
        var outcome = Result.Success(2).Ensure(static value => value > 5, TooSmallMessage);

        Assert.Equal("Too small.", outcome.GetError().Message);
    }

    [Fact]
    internal async Task Ensure_with_an_async_condition_and_a_synchronous_factory_returns_a_task()
    {
        var task = AsTask(Result.Success(2).Ensure(
            static async value =>
            {
                await Task.Yield();

                return value > 5;
            },
            static _ => Rejected));

        var outcome = await task;

        Assert.Same(Rejected, outcome.GetError());
    }

    [Fact]
    internal async Task Ensure_with_a_synchronous_condition_and_an_async_factory_returns_a_task()
    {
        var task = AsTask(Result.Success(2).Ensure(
            static value => value > 5,
            static async _ =>
            {
                await Task.Yield();

                return Rejected;
            }));

        var outcome = await task;

        Assert.Same(Rejected, outcome.GetError());
    }

    [Fact]
    internal async Task Ensure_with_two_async_lambdas_binds_to_the_task_overload()
    {
        var task = AsTask(Result.Success(2).Ensure(
            static async value =>
            {
                await Task.Yield();

                return value > 5;
            },
            static async value =>
            {
                await Task.Yield();

                return "Too small: " + value.ToString(System.Globalization.CultureInfo.InvariantCulture);
            }));

        var outcome = await task;

        Assert.Equal("Too small: 2", outcome.GetError().Message);
    }

    [Fact]
    internal async Task Ensure_with_value_tasks_for_both_delegates_returns_a_value_task()
    {
        var task = AsValueTask(Result.Success(2).Ensure(
            static value => new ValueTask<bool>(value > 5),
            static _ => new ValueTask<Error>(Rejected)));

        var outcome = await task;

        Assert.Same(Rejected, outcome.GetError());
    }

    [Fact]
    internal async Task Ensure_with_a_task_receiver_creates_the_error_only_when_the_condition_does_not_hold()
    {
        var calls = 0;

        var outcome = await Task.FromResult(Result.Success(2)).Ensure(
            static value => value > 1,
            _ =>
            {
                calls++;

                return "Never.";
            });

        Assert.Multiple(
            () => Assert.Equal(2, outcome.GetValue()),
            () => Assert.Equal(0, calls));
    }

    [Fact]
    internal void Ensure_throws_when_the_error_factory_returns_null()
    {
        var result = Result.Success(2);

        var exception = Record.Exception(() => result.Ensure(static value => value > 5, NoError));

        Assert.IsType<ArgumentNullException>(exception);
    }

    [Fact]
    internal void Ensure_throws_when_the_message_factory_returns_an_empty_message()
    {
        var result = Result.Success(2);

        var exception = Record.Exception(() => result.Ensure(static value => value > 5, static _ => string.Empty));

        Assert.IsType<ArgumentException>(exception);
    }

    [Fact]
    internal void Ensure_throws_when_the_condition_is_null()
    {
        var result = Result.Success(2);

        var exception = Record.Exception(() => result.Ensure((Func<int, bool>)null!, static _ => Rejected));

        Assert.Equal("predicate", Assert.IsType<ArgumentNullException>(exception).ParamName);
    }

    [Fact]
    internal void Ensure_throws_when_the_error_factory_is_null()
    {
        var result = Result.Success(2);

        var exception = Record.Exception(() => result.Ensure(static value => value > 5, (Func<int, Error>)null!));

        Assert.Equal("errorFactory", Assert.IsType<ArgumentNullException>(exception).ParamName);
    }

    [Fact]
    internal void Ensure_throws_when_the_message_factory_is_null()
    {
        var result = Result.Success(2);

        var exception = Record.Exception(() => result.Ensure(static value => value > 5, (Func<int, string>)null!));

        Assert.Equal("messageFactory", Assert.IsType<ArgumentNullException>(exception).ParamName);
    }

    [Fact]
    internal void Ensure_with_a_condition_and_an_error_still_binds()
    {
        var outcome = Result.Success(2).Ensure(static value => value > 5, Rejected);

        Assert.Same(Rejected, outcome.GetError());
    }

    // Passing the call through a parameter of an exact type pins the type the overload returns.
    private static Task<Result<int>> AsTask(Task<Result<int>> task) => task;

    private static ValueTask<Result<int>> AsValueTask(ValueTask<Result<int>> task) => task;

    private static Error TooSmall(int value) => Error.Create("Too small.").WithDetail("value", value);

    private static string TooSmallMessage(int value) => value > 0 ? "Too small." : "Not positive.";

    private static Error NoError(int value) => value > 0 ? null! : Rejected;
}
