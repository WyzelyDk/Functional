namespace Wyzely.Functional.ResultTests;

public sealed class ResultFlattenTests
{
    private static readonly Error Outer = Error.Create("Outer failed.");
    private static readonly Error Inner = Error.Create("Inner failed.");

    [Fact]
    internal void Flatten_returns_the_inner_success_when_both_levels_succeeded()
    {
        var flat = Result.Success(Result.Success(7)).Flatten();

        Assert.Equal(7, flat.GetValue());
    }

    [Fact]
    internal void Flatten_returns_the_inner_error_when_only_the_inner_result_failed()
    {
        var flat = Result.Success(Result.Failure<int>(Inner)).Flatten();

        Assert.Same(Inner, flat.GetError());
    }

    [Fact]
    internal void Flatten_returns_the_outer_error_when_the_outer_result_failed()
    {
        var flat = Result.Failure<Result<int>>(Outer).Flatten();

        Assert.Same(Outer, flat.GetError());
    }

    [Fact]
    internal void Flatten_of_a_default_outer_result_is_a_success_with_the_default_inner_value()
    {
        var flat = default(Result<Result<int>>).Flatten();

        Assert.Equal(0, flat.GetValue());
    }

    [Fact]
    internal async Task Flatten_on_a_completed_task_removes_one_level()
    {
        var success = await Task.FromResult(Result.Success(Result.Success(7))).Flatten();
        var outer = await Task.FromResult(Result.Failure<Result<int>>(Outer)).Flatten();

        Assert.Multiple(
            () => Assert.Equal(7, success.GetValue()),
            () => Assert.Same(Outer, outer.GetError()));
    }

    [Fact]
    internal async Task Flatten_on_a_task_that_completes_later_removes_one_level()
    {
        var flat = await Pending.Of(Result.Success(Result.Failure<int>(Inner))).Flatten();

        Assert.Same(Inner, flat.GetError());
    }

    [Fact]
    internal async Task Flatten_on_a_value_task_removes_one_level()
    {
        var success = await new ValueTask<Result<Result<int>>>(Result.Success(Result.Success(7))).Flatten();
        var later = await new ValueTask<Result<Result<int>>>(Pending.Of(Result.Success(Result.Failure<int>(Inner)))).Flatten();

        Assert.Multiple(
            () => Assert.Equal(7, success.GetValue()),
            () => Assert.Same(Inner, later.GetError()));
    }

    [Fact]
    internal void Flatten_on_a_null_task_throws()
    {
        Assert.Throws<ArgumentNullException>(() => { _ = ((Task<Result<Result<int>>>)null!).Flatten(); });
    }
}
