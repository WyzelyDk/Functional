namespace Wyzely.Functional.MaybeTests;

// The generated matrix tests cover every receiver and delegate variant of ValueOr; these tests cover how a call binds to them from consumer code.
public sealed class ValueOrTests
{
    [Fact]
    internal void A_lambda_computes_the_fallback_of_a_maybe_without_a_value()
    {
        var outcome = Maybe.None<int>().ValueOr(static () => 5);

        Assert.Equal(5, outcome);
    }

    [Fact]
    internal void A_method_group_computes_the_fallback_of_a_maybe_without_a_value()
    {
        var outcome = Maybe.None<int>().ValueOr(Eight);

        Assert.Equal(8, outcome);
    }

    [Fact]
    internal async Task An_asynchronous_lambda_gives_a_task_of_the_fallback()
    {
        var outcome = await Maybe.None<int>().ValueOr(static async () =>
        {
            await Task.Yield();

            return 7;
        });

        Assert.Equal(7, outcome);
    }

    [Fact]
    internal async Task A_task_of_a_maybe_without_a_value_takes_a_fallback_value()
    {
        var outcome = await Task.FromResult(Maybe.None<int>()).ValueOr(4);

        Assert.Equal(4, outcome);
    }

    [Fact]
    internal async Task A_value_task_of_a_maybe_without_a_value_takes_a_fallback_value()
    {
        var outcome = await new ValueTask<Maybe<int>>(Maybe.None<int>()).ValueOr(2);

        Assert.Equal(2, outcome);
    }

    [Fact]
    internal async Task A_task_of_a_maybe_without_a_value_takes_a_lambda_that_computes_the_fallback()
    {
        var outcome = await Task.FromResult(Maybe.None<int>()).ValueOr(static () => 3);

        Assert.Equal(3, outcome);
    }

    [Fact]
    internal void A_maybe_with_a_value_gives_its_value_without_computing_the_fallback()
    {
        var calls = 0;

        var outcome = Maybe.Some(2).ValueOr(() =>
        {
            calls++;

            return 9;
        });

        Assert.Multiple(
            () => Assert.Equal(2, outcome),
            () => Assert.Equal(0, calls));
    }

    // The instance method ValueOr(T) wins when it applies. For a maybe of object a lambda without parameters has a natural type that converts to object,
    // so it is taken as the fallback value itself.
    [Fact]
    internal void For_a_maybe_of_object_a_lambda_is_taken_as_the_fallback_value()
    {
        var outcome = Maybe.None<object>().ValueOr(static () => 5);

        Assert.IsType<Func<int>>(outcome);
    }

    // A null or default literal converts to a function as well as to the value, and for a Task or a ValueTask of a maybe the function wins:
    // a null function is rejected when the call is made.
    [Fact]
    internal async Task A_null_argument_for_a_task_of_a_maybe_binds_to_the_function_overload_and_is_rejected()
    {
        var receiver = Task.FromResult(Maybe.None<string>());

        await Assert.ThrowsAsync<ArgumentNullException>(() => receiver.ValueOr(null!));
    }

    [Fact]
    internal async Task A_default_argument_for_a_task_of_a_maybe_binds_to_the_function_overload_and_is_rejected()
    {
        var receiver = Task.FromResult(Maybe.None<int>());

        await Assert.ThrowsAsync<ArgumentNullException>(() => receiver.ValueOr(default!));
    }

    // For a Task or a ValueTask of a maybe the overload that takes the value does not take a lambda, so a lambda computes the fallback, also for object.
    [Fact]
    internal async Task For_a_task_of_a_maybe_of_object_a_lambda_computes_the_fallback()
    {
        var outcome = await Task.FromResult(Maybe.None<object>()).ValueOr(static () => 5);

        Assert.Equal(5, outcome);
    }

    private static int Eight() => 8;
}
