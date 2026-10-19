namespace Wyzely.Functional.Tests;

public sealed class MaybeToInvertedResultTests
{
    private static readonly Error Taken = Error.Conflict("The email is already registered.");

    [Fact]
    internal void ToInvertedResult_fails_with_the_given_error_when_the_maybe_has_a_value()
    {
        var result = Maybe.Some("ada@example.com").ToInvertedResult(Taken);

        Assert.Multiple(
            () => Assert.True(result.IsFailure),
            () => Assert.Same(Taken, result.GetError()));
    }

    [Fact]
    internal void ToInvertedResult_succeeds_when_the_maybe_has_no_value()
    {
        var result = Maybe.None<string>().ToInvertedResult(Taken);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    internal void ToInvertedResult_with_a_factory_builds_the_error_from_the_value()
    {
        var result = Maybe.Some("ada@example.com").ToInvertedResult(static email => Error.Conflict($"{email} is taken."));

        Assert.Equal("ada@example.com is taken.", result.GetError().Message);
    }

    [Fact]
    internal void ToInvertedResult_with_a_factory_does_not_call_it_when_the_maybe_has_no_value()
    {
        var calls = 0;

        var result = Maybe.None<string>().ToInvertedResult(email =>
        {
            calls++;

            return Error.Conflict(email);
        });

        Assert.Multiple(
            () => Assert.True(result.IsSuccess),
            () => Assert.Equal(0, calls));
    }

    [Fact]
    internal void ToInvertedResult_throws_when_the_error_or_the_factory_is_null()
    {
        Assert.Multiple(
            () => Assert.Throws<ArgumentNullException>(() => Maybe.Some(1).ToInvertedResult((Error)null!)),
            () => Assert.Throws<ArgumentNullException>(() => Maybe.Some(1).ToInvertedResult((Func<int, Error>)null!)),
            () => Assert.Throws<ArgumentNullException>(() => Maybe.Some(1).ToInvertedResult(static _ => null!)));
    }

    [Fact]
    internal async Task ToInvertedResult_on_a_completed_task_inverts_the_maybe()
    {
        var found = await Task.FromResult(Maybe.Some(1)).ToInvertedResult(Taken);
        var missing = await Task.FromResult(Maybe.None<int>()).ToInvertedResult(Taken);

        Assert.Multiple(
            () => Assert.Same(Taken, found.GetError()),
            () => Assert.True(missing.IsSuccess));
    }

    [Fact]
    internal async Task ToInvertedResult_on_a_task_that_completes_later_inverts_the_maybe()
    {
        var found = await Pending.Of(Maybe.Some(1)).ToInvertedResult(static id => Error.Conflict($"{id} exists."));
        var missing = await Pending.Of(Maybe.None<int>()).ToInvertedResult(Taken);

        Assert.Multiple(
            () => Assert.Equal("1 exists.", found.GetError().Message),
            () => Assert.True(missing.IsSuccess));
    }

    [Fact]
    internal async Task ToInvertedResult_on_a_value_task_inverts_the_maybe()
    {
        var found = await new ValueTask<Maybe<int>>(Maybe.Some(1)).ToInvertedResult(Taken);
        var missing = await new ValueTask<Maybe<int>>(Pending.Of(Maybe.None<int>())).ToInvertedResult(static id => Error.Conflict($"{id} exists."));

        Assert.Multiple(
            () => Assert.Same(Taken, found.GetError()),
            () => Assert.True(missing.IsSuccess));
    }

    [Fact]
    internal async Task ToInvertedResult_on_a_faulted_task_lets_the_exception_travel()
    {
        var faulted = Task.FromException<Maybe<int>>(new InvalidOperationException("Boom."));

        await Assert.ThrowsAsync<InvalidOperationException>(() => faulted.ToInvertedResult(Taken));
    }

    [Fact]
    internal void ToInvertedResult_on_a_task_throws_when_the_task_the_error_or_the_factory_is_null()
    {
        Assert.Multiple(
            () => Assert.Throws<ArgumentNullException>(() => { _ = ((Task<Maybe<int>>)null!).ToInvertedResult(Taken); }),
            () => Assert.Throws<ArgumentNullException>(() => { _ = Task.FromResult(Maybe.Some(1)).ToInvertedResult((Error)null!); }),
            () => Assert.Throws<ArgumentNullException>(() => { _ = Task.FromResult(Maybe.Some(1)).ToInvertedResult((Func<int, Error>)null!); }),
            () => Assert.Throws<ArgumentNullException>(() => { _ = new ValueTask<Maybe<int>>(Maybe.Some(1)).ToInvertedResult((Error)null!); }));
    }

    [Fact]
    internal async Task A_lookup_that_must_find_nothing_continues_only_when_nothing_was_found()
    {
        var created = await FindUserAsync("new@example.com")
            .ToInvertedResult(static user => Error.Conflict($"{user} is already registered."))
            .Map(static () => "created");

        var refused = await FindUserAsync("ada@example.com")
            .ToInvertedResult(static user => Error.Conflict($"{user} is already registered."))
            .Map(static () => "created");

        Assert.Multiple(
            () => Assert.Equal("created", created.GetValue()),
            () => Assert.Equal("ada@example.com is already registered.", refused.GetError().Message));
    }

    [Fact]
    internal async Task ToInvertedResult_with_an_error_on_a_value_task_waits_for_a_maybe_that_is_not_completed()
    {
        var found = await new ValueTask<Maybe<int>>(Pending.Of(Maybe.Some(1))).ToInvertedResult(Taken);
        var missing = await new ValueTask<Maybe<int>>(Pending.Of(Maybe.None<int>())).ToInvertedResult(Taken);

        Assert.Multiple(
            () => Assert.Same(Taken, found.GetError()),
            () => Assert.True(missing.IsSuccess));
    }

    [Fact]
    internal async Task ToInvertedResult_with_an_error_factory_on_a_value_task_that_is_already_completed_inverts_the_maybe()
    {
        var found = await new ValueTask<Maybe<int>>(Maybe.Some(1)).ToInvertedResult(static id => Error.Conflict($"{id} exists."));
        var missing = await new ValueTask<Maybe<int>>(Maybe.None<int>()).ToInvertedResult(static id => Error.Conflict($"{id} exists."));

        Assert.Multiple(
            () => Assert.Equal("1 exists.", found.GetError().Message),
            () => Assert.True(missing.IsSuccess));
    }

    private static Task<Maybe<string>> FindUserAsync(string email) =>
        Task.FromResult(email == "ada@example.com" ? Maybe.Some(email) : Maybe.None<string>());
}
