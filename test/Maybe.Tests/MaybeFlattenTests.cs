namespace Wyzely.Functional.MaybeTests;

public sealed class MaybeFlattenTests
{
    [Fact]
    internal void Flatten_returns_the_inner_maybe()
    {
        Assert.Multiple(
            () => Assert.Equal(1, Maybe.Some(Maybe.Some(1)).Flatten().GetValue()),
            () => Assert.True(Maybe.Some(Maybe.None<int>()).Flatten().IsNone),
            () => Assert.True(Maybe.None<Maybe<int>>().Flatten().IsNone),
            () => Assert.True(default(Maybe<Maybe<int>>).Flatten().IsNone));
    }

    [Fact]
    internal async Task Flatten_on_a_completed_task_removes_one_level()
    {
        var some = await Task.FromResult(Maybe.Some(Maybe.Some(1))).Flatten();
        var none = await Task.FromResult(Maybe.Some(Maybe.None<int>())).Flatten();

        Assert.Multiple(
            () => Assert.Equal(1, some.GetValue()),
            () => Assert.True(none.IsNone));
    }

    [Fact]
    internal async Task Flatten_on_a_task_that_completes_later_removes_one_level()
    {
        var some = await Pending.Of(Maybe.Some(Maybe.Some(1))).Flatten();
        var none = await Pending.Of(Maybe.None<Maybe<int>>()).Flatten();

        Assert.Multiple(
            () => Assert.Equal(1, some.GetValue()),
            () => Assert.True(none.IsNone));
    }

    [Fact]
    internal async Task Flatten_on_a_value_task_removes_one_level()
    {
        var some = await new ValueTask<Maybe<Maybe<int>>>(Maybe.Some(Maybe.Some(1))).Flatten();
        var later = await new ValueTask<Maybe<Maybe<int>>>(Pending.Of(Maybe.Some(Maybe.None<int>()))).Flatten();

        Assert.Multiple(
            () => Assert.Equal(1, some.GetValue()),
            () => Assert.True(later.IsNone));
    }

    [Fact]
    internal void Flatten_on_a_null_task_throws()
    {
        Assert.Throws<ArgumentNullException>(() => { _ = ((Task<Maybe<Maybe<int>>>)null!).Flatten(); });
    }
}
