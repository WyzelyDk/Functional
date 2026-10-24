namespace Wyzely.Functional.Tests;

// EnsureSome turns a result that holds a maybe into a result that holds the value, like EnsureNotNull does for a nullable value. An absent value
// becomes the error you give; a failure passes through.
public sealed class EnsureSomeTests
{
    private static readonly Error Missing = Error.Create("Missing.");
    private static readonly Error Failed = Error.Create("Failed.");

    [Fact]
    internal void EnsureSome_unwraps_a_maybe_that_has_a_value()
    {
        var outcome = Result.Success(Maybe.Some(2)).EnsureSome(Missing);

        Assert.Equal(2, outcome.GetValue());
    }

    [Fact]
    internal void EnsureSome_fails_with_the_given_error_when_the_maybe_has_no_value()
    {
        var outcome = Result.Success(Maybe.None<int>()).EnsureSome(Missing);

        Assert.Same(Missing, outcome.GetError());
    }

    [Fact]
    internal void EnsureSome_keeps_the_error_of_a_failed_result()
    {
        var outcome = Result.Failure<Maybe<int>>(Failed).EnsureSome(Missing);

        Assert.Same(Failed, outcome.GetError());
    }

    [Fact]
    internal void EnsureSome_with_a_factory_creates_the_error_only_when_the_maybe_has_no_value()
    {
        var calls = 0;

        Error Create()
        {
            calls++;

            return Missing;
        }

        var present = Result.Success(Maybe.Some(2)).EnsureSome(Create);
        var failed = Result.Failure<Maybe<int>>(Failed).EnsureSome(Create);
        var absent = Result.Success(Maybe.None<int>()).EnsureSome(Create);

        Assert.Multiple(
            () => Assert.Equal(2, present.GetValue()),
            () => Assert.Same(Failed, failed.GetError()),
            () => Assert.Same(Missing, absent.GetError()),
            () => Assert.Equal(1, calls));
    }

    [Fact]
    internal async Task EnsureSome_accepts_a_task_receiver_that_is_already_completed_or_completes_later()
    {
        var completed = await Task.FromResult(Result.Success(Maybe.Some(2))).EnsureSome(Missing);
        var later = await Pending.Of(Result.Success(Maybe.None<int>())).EnsureSome(Missing);
        var failed = await Pending.Of(Result.Failure<Maybe<int>>(Failed)).EnsureSome(Missing);

        Assert.Multiple(
            () => Assert.Equal(2, completed.GetValue()),
            () => Assert.Same(Missing, later.GetError()),
            () => Assert.Same(Failed, failed.GetError()));
    }

    [Fact]
    internal async Task EnsureSome_accepts_a_value_task_receiver_that_is_already_completed_or_completes_later()
    {
        var completed = await new ValueTask<Result<Maybe<int>>>(Result.Success(Maybe.Some(2))).EnsureSome(Missing);
        var later = await new ValueTask<Result<Maybe<int>>>(Pending.Of(Result.Success(Maybe.None<int>()))).EnsureSome(Missing);

        Assert.Multiple(
            () => Assert.Equal(2, completed.GetValue()),
            () => Assert.Same(Missing, later.GetError()));
    }

    [Fact]
    internal async Task EnsureSome_with_a_factory_accepts_a_task_and_a_value_task_receiver()
    {
        var task = await Pending.Of(Result.Success(Maybe.None<int>())).EnsureSome(static () => Missing);
        var valueTask = await new ValueTask<Result<Maybe<int>>>(Result.Success(Maybe.None<int>())).EnsureSome(static () => Missing);

        Assert.Multiple(
            () => Assert.Same(Missing, task.GetError()),
            () => Assert.Same(Missing, valueTask.GetError()));
    }

    [Fact]
    internal async Task EnsureSome_with_a_factory_takes_the_fast_path_for_a_completed_task_and_waits_for_a_pending_value_task()
    {
        var completed = await Task.FromResult(Result.Success(Maybe.Some(2))).EnsureSome(static () => Missing);

        var source = new TaskCompletionSource<Result<Maybe<int>>>(TaskCreationOptions.RunContinuationsAsynchronously);
        var waiting = new ValueTask<Result<Maybe<int>>>(source.Task).EnsureSome(static () => Missing);
        var stillWaiting = !waiting.IsCompleted;
        source.SetResult(Result.Success(Maybe.Some(3)));
        var pending = await waiting;

        Assert.Multiple(
            () => Assert.Equal(2, completed.GetValue()),
            () => Assert.True(stillWaiting),
            () => Assert.Equal(3, pending.GetValue()));
    }

    [Fact]
    internal async Task EnsureSome_ends_a_chain_without_an_await_in_the_middle()
    {
        var outcome = await Task.FromResult(Result.Success(Maybe.Some(2)))
            .EnsureSome(Missing)
            .Map(static number => number * 10);

        Assert.Equal(20, outcome.GetValue());
    }

    [Fact]
    internal void EnsureSome_throws_when_the_error_or_the_task_is_null()
    {
        var result = Result.Success(Maybe.Some(2));

        var noError = Record.Exception(() => result.EnsureSome((Error)null!));
        var noFactory = Record.Exception(() => result.EnsureSome((Func<Error>)null!));
        var noTask = Record.Exception(() => (object)((Task<Result<Maybe<int>>>)null!).EnsureSome(Missing));

        Assert.Multiple(
            () => Assert.Equal("error", Assert.IsType<ArgumentNullException>(noError).ParamName),
            () => Assert.Equal("error", Assert.IsType<ArgumentNullException>(noFactory).ParamName),
            () => Assert.Equal("result", Assert.IsType<ArgumentNullException>(noTask).ParamName));
    }
}
