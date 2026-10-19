namespace Wyzely.Functional.Tests;

public sealed class MaybeToResultTests
{
    private static readonly Error Missing = Error.NotFound("No such customer.");

    [Fact]
    internal void ToResult_returns_a_success_with_the_value_when_the_maybe_has_one()
    {
        var result = Maybe.Some(5).ToResult(Missing);

        Assert.Multiple(
            () => Assert.True(result.IsSuccess),
            () => Assert.Equal(5, result.GetValue()));
    }

    [Fact]
    internal void ToResult_returns_a_failure_with_the_given_error_when_the_maybe_has_none()
    {
        var result = Maybe.None<int>().ToResult(Missing);

        Assert.Multiple(
            () => Assert.True(result.IsFailure),
            () => Assert.Same(Missing, result.GetError()));
    }

    [Fact]
    internal void ToResult_throws_when_the_error_is_null()
    {
        Assert.Throws<ArgumentNullException>(() => Maybe.Some(1).ToResult((Error)null!));
    }

    [Fact]
    internal void ToResult_with_a_factory_does_not_call_it_when_the_maybe_has_a_value()
    {
        var calls = 0;

        var result = Maybe.Some(5).ToResult(() =>
        {
            calls++;

            return Missing;
        });

        Assert.Multiple(
            () => Assert.Equal(5, result.GetValue()),
            () => Assert.Equal(0, calls));
    }

    [Fact]
    internal void ToResult_with_a_factory_calls_it_once_when_the_maybe_has_none()
    {
        var calls = 0;

        var result = Maybe.None<int>().ToResult(() =>
        {
            calls++;

            return Missing;
        });

        Assert.Multiple(
            () => Assert.Same(Missing, result.GetError()),
            () => Assert.Equal(1, calls));
    }

    [Fact]
    internal void ToResult_with_a_factory_throws_when_the_factory_is_null()
    {
        Assert.Throws<ArgumentNullException>(() => Maybe.Some(1).ToResult((Func<Error>)null!));
    }

    [Fact]
    internal async Task ToResult_on_a_completed_task_converts_the_maybe()
    {
        var some = await Task.FromResult(Maybe.Some(5)).ToResult(Missing);
        var none = await Task.FromResult(Maybe.None<int>()).ToResult(Missing);

        Assert.Multiple(
            () => Assert.Equal(5, some.GetValue()),
            () => Assert.Same(Missing, none.GetError()));
    }

    [Fact]
    internal async Task ToResult_on_a_task_that_completes_later_converts_the_maybe()
    {
        var some = await Pending.Of(Maybe.Some(5)).ToResult(Missing);
        var none = await Pending.Of(Maybe.None<int>()).ToResult(() => Missing);

        Assert.Multiple(
            () => Assert.Equal(5, some.GetValue()),
            () => Assert.Same(Missing, none.GetError()));
    }

    [Fact]
    internal async Task ToResult_on_a_value_task_converts_the_maybe()
    {
        var some = await new ValueTask<Maybe<int>>(Maybe.Some(5)).ToResult(Missing);
        var none = await new ValueTask<Maybe<int>>(Pending.Of(Maybe.None<int>())).ToResult(() => Missing);

        Assert.Multiple(
            () => Assert.Equal(5, some.GetValue()),
            () => Assert.Same(Missing, none.GetError()));
    }

    [Fact]
    internal async Task ToResult_on_a_faulted_task_lets_the_exception_travel()
    {
        var faulted = Task.FromException<Maybe<int>>(new InvalidOperationException("Boom."));

        await Assert.ThrowsAsync<InvalidOperationException>(() => faulted.ToResult(Missing));
    }

    [Fact]
    internal void ToResult_on_a_task_throws_when_the_task_or_the_error_is_null()
    {
        Assert.Multiple(
            () => Assert.Throws<ArgumentNullException>(() => { _ = ((Task<Maybe<int>>)null!).ToResult(Missing); }),
            () => Assert.Throws<ArgumentNullException>(() => { _ = Task.FromResult(Maybe.Some(1)).ToResult((Error)null!); }),
            () => Assert.Throws<ArgumentNullException>(() => { _ = Task.FromResult(Maybe.Some(1)).ToResult((Func<Error>)null!); }),
            () => Assert.Throws<ArgumentNullException>(() => { _ = new ValueTask<Maybe<int>>(Maybe.Some(1)).ToResult((Error)null!); }));
    }

    [Fact]
    internal async Task ToResult_with_an_error_on_a_value_task_waits_for_a_maybe_that_is_not_completed()
    {
        var some = await new ValueTask<Maybe<int>>(Pending.Of(Maybe.Some(5))).ToResult(Missing);
        var none = await new ValueTask<Maybe<int>>(Pending.Of(Maybe.None<int>())).ToResult(Missing);

        Assert.Multiple(
            () => Assert.Equal(5, some.GetValue()),
            () => Assert.Same(Missing, none.GetError()));
    }

    [Fact]
    internal async Task ToResult_with_an_error_factory_on_a_value_task_that_is_already_completed_converts_the_maybe()
    {
        var some = await new ValueTask<Maybe<int>>(Maybe.Some(5)).ToResult(() => Missing);
        var none = await new ValueTask<Maybe<int>>(Maybe.None<int>()).ToResult(() => Missing);

        Assert.Multiple(
            () => Assert.Equal(5, some.GetValue()),
            () => Assert.Same(Missing, none.GetError()));
    }

    [Fact]
    internal async Task ToResult_with_an_error_factory_on_a_completed_task_converts_the_maybe()
    {
        var some = await Task.FromResult(Maybe.Some(5)).ToResult(() => Missing);
        var none = await Task.FromResult(Maybe.None<int>()).ToResult(() => Missing);

        Assert.Multiple(
            () => Assert.Equal(5, some.GetValue()),
            () => Assert.Same(Missing, none.GetError()));
    }
}
