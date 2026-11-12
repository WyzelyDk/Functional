namespace Wyzely.Functional.MaybeTests;

// OfType narrows a maybe to a maybe of a type that its value can have, like Enumerable.OfType does for a sequence: a value of another type is dropped.
public sealed class OfTypeTests
{
    [Fact]
    internal void A_value_of_the_type_is_kept()
    {
        var outcome = Maybe.Some<object>("text").OfType<string>();

        Assert.Equal("text", outcome.GetValue());
    }

    [Fact]
    internal void A_value_of_another_type_gives_none()
    {
        var outcome = Maybe.Some<object>("text").OfType<int>();

        Assert.False(outcome.IsSome);
    }

    [Fact]
    internal void A_maybe_without_a_value_gives_none()
    {
        var outcome = Maybe.None<object>().OfType<string>();

        Assert.False(outcome.IsSome);
    }

    [Fact]
    internal void A_value_is_found_through_its_base_type()
    {
        var dog = new Dog();

        var outcome = Maybe.Some<Animal>(dog).OfType<Dog>();

        Assert.Same(dog, outcome.GetValue());
    }

    [Fact]
    internal void A_value_is_not_found_as_a_sibling_type()
    {
        var outcome = Maybe.Some<Animal>(new Dog()).OfType<Cat>();

        Assert.False(outcome.IsSome);
    }

    [Fact]
    internal void An_interface_of_the_value_is_a_type_of_the_value()
    {
        var outcome = Maybe.Some<object>("text").OfType<IComparable>();

        Assert.Equal("text", outcome.GetValue());
    }

    [Fact]
    internal void A_boxed_value_type_is_unboxed_to_its_own_type_only()
    {
        var boxed = Maybe.Some<object>(5);

        Assert.Multiple(
            () => Assert.Equal(5, boxed.OfType<int>().GetValue()),
            () => Assert.False(boxed.OfType<long>().IsSome));
    }

    [Fact]
    internal void A_value_type_can_be_narrowed_to_object()
    {
        var outcome = Maybe.Some(5).OfType<object>();

        Assert.Equal(5, outcome.GetValue());
    }

    [Fact]
    internal async Task A_completed_task_is_narrowed()
    {
        var kept = await Task.FromResult(Maybe.Some<object>("text")).OfType<object, string>();
        var dropped = await Task.FromResult(Maybe.Some<object>("text")).OfType<object, int>();
        var none = await Task.FromResult(Maybe.None<object>()).OfType<object, string>();

        Assert.Multiple(
            () => Assert.Equal("text", kept.GetValue()),
            () => Assert.False(dropped.IsSome),
            () => Assert.False(none.IsSome));
    }

    [Fact]
    internal async Task A_pending_task_is_narrowed_when_it_completes()
    {
        var source = new TaskCompletionSource<Maybe<object>>(TaskCreationOptions.RunContinuationsAsynchronously);

        var pending = source.Task.OfType<object, int>();
        var wasPending = !pending.IsCompleted;
        source.SetResult(Maybe.Some<object>(5));
        var outcome = await pending;

        Assert.Multiple(
            () => Assert.True(wasPending),
            () => Assert.Equal(5, outcome.GetValue()));
    }

    [Fact]
    internal void A_null_task_is_rejected()
    {
        Task<Maybe<object>> missing = null!;

        Assert.Throws<ArgumentNullException>(Call);

        void Call() => _ = missing.OfType<object, string>();
    }

    [Fact]
    internal async Task A_completed_value_task_is_narrowed()
    {
        var kept = await new ValueTask<Maybe<object>>(Maybe.Some<object>("text")).OfType<object, string>();
        var dropped = await new ValueTask<Maybe<object>>(Maybe.Some<object>("text")).OfType<object, int>();
        var none = await default(ValueTask<Maybe<object>>).OfType<object, string>();

        Assert.Multiple(
            () => Assert.Equal("text", kept.GetValue()),
            () => Assert.False(dropped.IsSome),
            () => Assert.False(none.IsSome));
    }

    [Fact]
    internal async Task A_pending_value_task_is_narrowed_when_it_completes()
    {
        var source = new TaskCompletionSource<Maybe<object>>(TaskCreationOptions.RunContinuationsAsynchronously);

        var pending = new ValueTask<Maybe<object>>(source.Task).OfType<object, int>();
        var wasPending = !pending.IsCompleted;
        source.SetResult(Maybe.Some<object>(5));
        var outcome = await pending;

        Assert.Multiple(
            () => Assert.True(wasPending),
            () => Assert.Equal(5, outcome.GetValue()));
    }

    private abstract class Animal
    {
        internal abstract string Sound { get; }
    }

    private sealed class Dog : Animal
    {
        internal override string Sound => "woof";
    }

    private sealed class Cat : Animal
    {
        internal override string Sound => "meow";
    }
}
