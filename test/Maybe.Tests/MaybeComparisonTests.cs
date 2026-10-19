namespace Wyzely.Functional.MaybeTests;

public sealed class MaybeComparisonTests
{
    [Fact]
    internal void CompareTo_sorts_none_before_some_and_compares_the_values_of_two_somes()
    {
        Assert.Multiple(
            () => Assert.True(Maybe.None<int>().CompareTo(Maybe.Some(1)) < 0),
            () => Assert.Equal(0, Maybe.None<int>().CompareTo(Maybe.None<int>())),
            () => Assert.True(Maybe.Some(1).CompareTo(Maybe.None<int>()) > 0),
            () => Assert.True(Maybe.Some(1).CompareTo(Maybe.Some(2)) < 0),
            () => Assert.Equal(0, Maybe.Some(2).CompareTo(Maybe.Some(2))),
            () => Assert.True(Maybe.Some(3).CompareTo(Maybe.Some(2)) > 0));
    }

    [Fact]
    internal void The_comparison_operators_follow_CompareTo()
    {
        var none = Maybe.None<int>();
        var one = Maybe.Some(1);
        var anotherOne = Maybe.Some(1);
        var two = Maybe.Some(2);

        Assert.Multiple(
            () => Assert.True(none < one),
            () => Assert.False(one < none),
            () => Assert.False(one < anotherOne),
            () => Assert.True(one <= two),
            () => Assert.True(one <= anotherOne),
            () => Assert.False(two <= one),
            () => Assert.True(two > one),
            () => Assert.False(one > anotherOne),
            () => Assert.False(none > one),
            () => Assert.True(two >= one),
            () => Assert.True(one >= anotherOne),
            () => Assert.False(none >= one));
    }

    [Fact]
    internal void Sorting_puts_none_first_and_the_somes_in_order_of_their_values()
    {
        var items = new[] { Maybe.Some(2), Maybe.None<int>(), Maybe.Some(1) };

        Array.Sort(items);

        Assert.Equal([Maybe.None<int>(), Maybe.Some(1), Maybe.Some(2)], items);
    }
}
