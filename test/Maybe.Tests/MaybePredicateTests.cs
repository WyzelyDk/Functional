namespace Wyzely.Functional.MaybeTests;

// Any, All and Contains leave the railway with a bool. These tests pin what a caller can write and the two answers that surprise people (a
// maybe without a value is not "any" and is "all"); the behavior of every variant is in the generated matrix tests.
public sealed class MaybePredicateTests
{
    [Fact]
    internal void Any_is_true_for_a_value_that_satisfies_the_condition()
    {
        Assert.True(Maybe.Some(2).Any(static value => value > 1));
    }

    [Fact]
    internal void Any_is_false_for_a_value_that_does_not_satisfy_the_condition()
    {
        Assert.False(Maybe.Some(2).Any(static value => value > 5));
    }

    [Fact]
    internal void Any_is_false_for_a_maybe_without_a_value()
    {
        Assert.False(Maybe.None<int>().Any(static _ => true));
    }

    [Fact]
    internal void All_is_true_for_a_maybe_without_a_value()
    {
        Assert.True(Maybe.None<int>().All(static _ => false));
    }

    [Fact]
    internal void All_is_true_for_a_value_that_satisfies_the_condition()
    {
        Assert.True(Maybe.Some(2).All(static value => value > 1));
    }

    [Fact]
    internal void All_is_false_for_a_value_that_does_not_satisfy_the_condition()
    {
        Assert.False(Maybe.Some(2).All(static value => value > 5));
    }

    [Fact]
    internal void Contains_compares_with_the_default_equality_of_the_type()
    {
        Assert.Multiple(
            () => Assert.True(Maybe.Some("a").Contains("a")),
            () => Assert.False(Maybe.Some("a").Contains("A")),
            () => Assert.True(Maybe.Some(new Point(1, 2)).Contains(new Point(1, 2))));
    }

    [Fact]
    internal void Contains_is_false_for_a_maybe_without_a_value()
    {
        Assert.False(Maybe.None<string>().Contains("a"));
    }

    [Fact]
    internal async Task Any_awaits_an_async_lambda()
    {
        var task = Maybe.Some(2).Any(static async value =>
        {
            await Task.Yield();

            return value > 1;
        });

        Assert.True(await task);
    }

    [Fact]
    internal async Task All_awaits_a_value_task_predicate()
    {
        var task = Maybe.Some(2).All(static value => new ValueTask<bool>(value > 5));

        Assert.False(await task);
    }

    [Fact]
    internal async Task Contains_accepts_a_task_receiver()
    {
        var receiver = Task.FromResult(Maybe.Some(2));

        Assert.True(await receiver.Contains(2));
    }

    [Fact]
    internal async Task Any_accepts_a_value_task_receiver_with_a_task_predicate()
    {
        var receiver = new ValueTask<Maybe<int>>(Maybe.Some(2));

        var answer = await receiver.Any(static value => Task.FromResult(value > 1));

        Assert.True(answer);
    }

    private sealed record Point(int X, int Y);
}
