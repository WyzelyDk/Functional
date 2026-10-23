namespace Wyzely.Functional.MaybeTests;

// Behavior of the railway that the generated matrix tests do not cover: null handling and argument checks.
public sealed class MaybeExtensionsTests
{
    [Fact]
    internal void Map_turns_a_null_result_of_the_delegate_into_none()
    {
        var mapped = Maybe.Some(1).Map(static _ => (string)null!);

        Assert.True(mapped.IsNone);
    }

    [Fact]
    internal async Task Map_turns_a_null_result_of_an_asynchronous_delegate_into_none()
    {
        var mapped = await Maybe.Some(1).Map(static async _ =>
        {
            await Task.Yield();

            return (string)null!;
        });

        Assert.True(mapped.IsNone);
    }

    [Fact]
    internal void Some_explains_what_to_do_instead_when_the_value_is_null()
    {
        var exception = Assert.Throws<ArgumentNullException>(() => Maybe.Some<string>(null!));

        Assert.Contains("Maybe.From", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    internal void SomeIf_does_not_check_the_value_when_the_condition_is_false()
    {
        Assert.Multiple(
            () => Assert.True(Maybe.SomeIf(false, (string)null!).IsNone),
            () => Assert.True(Maybe.NoneIf(true, (string)null!).IsNone),
            () => Assert.Throws<ArgumentNullException>(() => Maybe.SomeIf(true, (string)null!)),
            () => Assert.Throws<ArgumentNullException>(() => Maybe.NoneIf(false, (string)null!)));
    }

    [Fact]
    internal void Every_operation_throws_when_its_delegate_is_null()
    {
        var maybe = Maybe.Some(1);

        Assert.Multiple(
            () => Assert.Throws<ArgumentNullException>(() => maybe.Map((Func<int, int>)null!)),
            () => Assert.Throws<ArgumentNullException>(() => maybe.Bind((Func<int, Maybe<int>>)null!)),
            () => Assert.Throws<ArgumentNullException>(() => maybe.Filter((Func<int, bool>)null!)),
            () => Assert.Throws<ArgumentNullException>(() => maybe.Tap((Action<int>)null!)),
            () => Assert.Throws<ArgumentNullException>(() => maybe.TapNone((Action)null!)),
            () => Assert.Throws<ArgumentNullException>(() => maybe.Or((Func<Maybe<int>>)null!)),
            () => Assert.Throws<ArgumentNullException>(() => maybe.ValueOr((Func<int>)null!)),
            () => Assert.Throws<ArgumentNullException>(() => maybe.Match(null!, static () => 0)),
            () => Assert.Throws<ArgumentNullException>(() => maybe.Match(static x => x, null!)),
            () => Assert.Throws<ArgumentNullException>(() => maybe.Switch(null!, static () => { })),
            () => Assert.Throws<ArgumentNullException>(() => maybe.Switch(static _ => { }, null!)));
    }

    [Fact]
    internal void An_asynchronous_receiver_that_is_null_throws()
    {
        Assert.Multiple(
            () => Assert.Throws<ArgumentNullException>(() => { _ = ((Task<Maybe<int>>)null!).Map(static x => x); }),
            () => Assert.Throws<ArgumentNullException>(() => { _ = ((Task<Maybe<int>>)null!).Match(static x => x, static () => 0); }));
    }

    [Fact]
    internal async Task A_faulted_receiver_lets_the_exception_travel()
    {
        var faulted = Task.FromException<Maybe<int>>(new InvalidOperationException("Boom."));

        await Assert.ThrowsAsync<InvalidOperationException>(() => faulted.Map(static x => x));
    }

    [Fact]
    internal async Task A_delegate_that_throws_lets_the_exception_travel()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() => Maybe.Some(1).Map(static Task<int> (_) => throw new InvalidOperationException("Boom.")));
    }
}
