namespace Wyzely.Functional.MaybeTests;

// A step on an already completed task that leaves the maybe as it was hands back the task it received instead of allocating a new one.
// The steps that can change the value (Map, Bind) must never do that, so they are pinned here as well.
public sealed class CompletedTaskReuseTests
{
    [Fact]
    internal void Filter_returns_the_receiver_task_when_the_value_satisfies_the_condition()
    {
        var receiver = Task.FromResult(Maybe.Some(2));

        var returned = receiver.Filter(static x => x > 1);

        Assert.Same(receiver, returned);
    }

    [Fact]
    internal void Filter_returns_the_receiver_task_when_the_receiver_is_none()
    {
        var receiver = Task.FromResult(Maybe.None<int>());

        var returned = receiver.Filter(static x => x > 1);

        Assert.Same(receiver, returned);
    }

    [Fact]
    internal void Filter_returns_a_new_task_with_none_when_the_value_does_not_satisfy_the_condition()
    {
        var receiver = Task.FromResult(Maybe.Some(2));

        var returned = receiver.Filter(static x => x > 5);

        Assert.Multiple(
            () => Assert.NotSame(receiver, returned),
            () => Assert.True(returned.Result.IsNone));
    }

    [Fact]
    internal void Tap_returns_the_receiver_task_for_a_value_and_for_none()
    {
        var some = Task.FromResult(Maybe.Some(2));
        var none = Task.FromResult(Maybe.None<int>());

        Assert.Multiple(
            () => Assert.Same(some, some.Tap(static _ => { })),
            () => Assert.Same(none, none.Tap(static _ => { })));
    }

    [Fact]
    internal void TapNone_returns_the_receiver_task_for_a_value_and_for_none()
    {
        var some = Task.FromResult(Maybe.Some(2));
        var none = Task.FromResult(Maybe.None<int>());

        Assert.Multiple(
            () => Assert.Same(some, some.TapNone(static () => { })),
            () => Assert.Same(none, none.TapNone(static () => { })));
    }

    [Fact]
    internal void Or_returns_the_receiver_task_when_it_has_a_value()
    {
        var receiver = Task.FromResult(Maybe.Some(2));

        var returned = receiver.Or(static () => Maybe.Some(7));

        Assert.Same(receiver, returned);
    }

    [Fact]
    internal void Or_returns_the_receiver_task_when_the_alternative_is_none_as_well()
    {
        var receiver = Task.FromResult(Maybe.None<int>());

        var returned = receiver.Or(static () => Maybe.None<int>());

        Assert.Same(receiver, returned);
    }

    [Fact]
    internal void Or_returns_a_new_task_with_the_alternative_when_the_receiver_is_none()
    {
        var receiver = Task.FromResult(Maybe.None<int>());

        var returned = receiver.Or(static () => Maybe.Some(7));

        Assert.Multiple(
            () => Assert.NotSame(receiver, returned),
            () => Assert.Equal(7, returned.Result.GetValue()));
    }

    [Fact]
    internal void Map_returns_a_task_with_the_new_value_even_though_the_maybe_has_a_value_either_way()
    {
        var receiver = Task.FromResult(Maybe.Some(2));

        var returned = receiver.Map(static x => x + 1);

        Assert.Multiple(
            () => Assert.NotSame(receiver, returned),
            () => Assert.Equal(3, returned.Result.GetValue()));
    }

    [Fact]
    internal void Bind_returns_a_task_with_the_new_value_even_though_the_maybe_has_a_value_either_way()
    {
        var receiver = Task.FromResult(Maybe.Some(2));

        var returned = receiver.Bind(static x => Maybe.Some(x * 10));

        Assert.Multiple(
            () => Assert.NotSame(receiver, returned),
            () => Assert.Equal(20, returned.Result.GetValue()));
    }

#if NET8_0_OR_GREATER
    [Fact]
    internal void A_chain_of_steps_that_leave_the_maybe_as_it_was_allocates_nothing_on_a_completed_task()
    {
        var receiver = Task.FromResult(Maybe.Some(2));

        RunChain(receiver, 10);
        var allocated = AllocationProbe.Smallest(() => RunChain(receiver, 1_000));

        Assert.Equal(0, allocated);
    }

    private static void RunChain(Task<Maybe<int>> receiver, int iterations)
    {
        for (var i = 0; i < iterations; i++)
        {
            _ = receiver
                .Filter(static x => x > 0)
                .Tap(static _ => { })
                .TapNone(static () => { })
                .Or(static () => Maybe.Some(0));
        }
    }
#endif
}
