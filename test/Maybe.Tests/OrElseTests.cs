namespace Wyzely.Functional.MaybeTests;

// The generated matrix tests cover every receiver and delegate variant of OrElse; these tests cover how a call binds to them from consumer code.
public sealed class OrElseTests
{
    [Fact]
    internal void A_lambda_that_returns_a_value_gives_a_maybe_that_holds_it()
    {
        var outcome = Maybe.None<int>().OrElse(static () => 5);

        Assert.Equal(5, outcome.GetValue());
    }

    [Fact]
    internal void A_lambda_that_returns_a_maybe_gives_that_maybe()
    {
        var outcome = Maybe.None<int>().OrElse(static () => Maybe.Some(6));

        Assert.Equal(6, outcome.GetValue());
    }

    [Fact]
    internal void A_lambda_that_returns_none_gives_none()
    {
        var outcome = Maybe.None<int>().OrElse(static () => Maybe.None<int>());

        Assert.False(outcome.IsSome);
    }

    [Fact]
    internal void A_method_group_that_returns_a_value_gives_a_maybe_that_holds_it()
    {
        var outcome = Maybe.None<int>().OrElse(Seven);

        Assert.Equal(7, outcome.GetValue());
    }

    [Fact]
    internal void A_method_group_that_returns_a_maybe_gives_that_maybe()
    {
        var outcome = Maybe.None<int>().OrElse(EightAsMaybe);

        Assert.Equal(8, outcome.GetValue());
    }

    [Fact]
    internal void A_null_reference_that_the_delegate_returns_leaves_the_maybe_without_a_value()
    {
        var outcome = Maybe.None<string>().OrElse(static () => (string)null!);

        Assert.False(outcome.IsSome);
    }

    [Fact]
    internal async Task An_asynchronous_lambda_that_returns_a_value_gives_a_task_of_a_maybe_that_holds_it()
    {
        var outcome = await Maybe.None<int>().OrElse(static async () =>
        {
            await Task.Yield();

            return 11;
        });

        Assert.Equal(11, outcome.GetValue());
    }

    [Fact]
    internal async Task An_asynchronous_lambda_that_returns_a_maybe_gives_a_task_of_that_maybe()
    {
        var outcome = await Maybe.None<int>().OrElse(static async () =>
        {
            await Task.Yield();

            return Maybe.Some(12);
        });

        Assert.Equal(12, outcome.GetValue());
    }

    [Fact]
    internal async Task A_task_of_none_takes_a_lambda_that_returns_a_value()
    {
        var outcome = await Task.FromResult(Maybe.None<int>()).OrElse(static () => 4);

        Assert.Equal(4, outcome.GetValue());
    }

    [Fact]
    internal void A_maybe_with_a_value_is_returned_without_running_the_delegate()
    {
        var calls = 0;

        var outcome = Maybe.Some(2).OrElse(() =>
        {
            calls++;

            return 9;
        });

        Assert.Multiple(
            () => Assert.Equal(2, outcome.GetValue()),
            () => Assert.Equal(0, calls));
    }

    private static int Seven() => 7;

    private static Maybe<int> EightAsMaybe() => Maybe.Some(8);
}
