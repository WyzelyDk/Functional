namespace Wyzely.Functional.Tests;

public sealed class ResultToMaybeTests
{
    [Fact]
    internal void ToMaybe_returns_some_for_a_success()
    {
        var maybe = Result.Success(5).ToMaybe();

        Assert.Equal(5, maybe.GetValue());
    }

    [Fact]
    internal void ToMaybe_returns_none_for_a_failure_and_drops_the_error()
    {
        var maybe = Result.Failure<int>(Error.Create("Boom.")).ToMaybe();

        Assert.True(maybe.IsNone);
    }

    [Fact]
    internal void ToMaybe_returns_none_for_a_success_whose_value_is_null()
    {
        var maybe = Result.Success<string>(null!).ToMaybe();

        Assert.True(maybe.IsNone);
    }

    [Fact]
    internal void ToMaybe_of_a_default_result_is_some_of_the_default_value()
    {
        Assert.Equal(0, default(Result<int>).ToMaybe().GetValue());
    }

    [Fact]
    internal async Task ToMaybe_on_a_completed_task_converts_the_result()
    {
        var some = await Task.FromResult(Result.Success(5)).ToMaybe();
        var none = await Task.FromResult(Result.Failure<int>("Boom.")).ToMaybe();

        Assert.Multiple(
            () => Assert.Equal(5, some.GetValue()),
            () => Assert.True(none.IsNone));
    }

    [Fact]
    internal async Task ToMaybe_on_a_task_that_completes_later_converts_the_result()
    {
        var some = await Pending.Of(Result.Success(5)).ToMaybe();
        var none = await Pending.Of(Result.Failure<int>("Boom.")).ToMaybe();

        Assert.Multiple(
            () => Assert.Equal(5, some.GetValue()),
            () => Assert.True(none.IsNone));
    }

    [Fact]
    internal async Task ToMaybe_on_a_value_task_converts_the_result()
    {
        var some = await new ValueTask<Result<int>>(Result.Success(5)).ToMaybe();
        var none = await new ValueTask<Result<int>>(Pending.Of(Result.Failure<int>("Boom."))).ToMaybe();

        Assert.Multiple(
            () => Assert.Equal(5, some.GetValue()),
            () => Assert.True(none.IsNone));
    }

    [Fact]
    internal void ToMaybe_on_a_task_throws_when_the_task_is_null()
    {
        Assert.Throws<ArgumentNullException>(() => { _ = ((Task<Result<int>>)null!).ToMaybe(); });
    }
}
