namespace Wyzely.Functional.MaybeTests;

public sealed class MaybeLinqTests
{
    [Fact]
    internal void Query_syntax_combines_values_and_stops_at_the_first_none()
    {
        var total = from price in Maybe.Some(40)
                    from tax in Maybe.Some(2)
                    select price + tax;

        var missing = from price in Maybe.Some(40)
                      from tax in Maybe.None<int>()
                      select price + tax;

        Assert.Multiple(
            () => Assert.Equal(42, total.GetValue()),
            () => Assert.True(missing.IsNone));
    }

    [Fact]
    internal void Where_keeps_a_value_that_satisfies_the_predicate()
    {
        var kept = from x in Maybe.Some(4) where x > 3 select x;
        var dropped = from x in Maybe.Some(2) where x > 3 select x;

        Assert.Multiple(
            () => Assert.Equal(4, kept.GetValue()),
            () => Assert.True(dropped.IsNone));
    }

    [Fact]
    internal void Select_is_map_under_the_name_that_query_syntax_requires()
    {
        Assert.Equal("5", Maybe.Some(5).Select(x => x.ToString(System.Globalization.CultureInfo.InvariantCulture)).GetValue());
    }

    [Fact]
    internal void Zip_combines_two_maybes_only_when_both_have_a_value()
    {
        Assert.Multiple(
            () => Assert.Equal((1, "a"), Maybe.Some(1).Zip(Maybe.Some("a")).GetValue()),
            () => Assert.True(Maybe.Some(1).Zip(Maybe.None<string>()).IsNone),
            () => Assert.True(Maybe.None<int>().Zip(Maybe.Some("a")).IsNone),
            () => Assert.Equal("1a", Maybe.Some(1).Zip(Maybe.Some("a"), (number, text) => number + text).GetValue()));
    }

    [Fact]
    internal void Flatten_removes_one_level_of_nesting()
    {
        Assert.Multiple(
            () => Assert.Equal(1, Maybe.Some(Maybe.Some(1)).Flatten().GetValue()),
            () => Assert.True(Maybe.Some(Maybe.None<int>()).Flatten().IsNone),
            () => Assert.True(Maybe.None<Maybe<int>>().Flatten().IsNone));
    }

    [Fact]
    internal void The_methods_throw_when_a_delegate_is_null()
    {
        Assert.Multiple(
            () => Assert.Throws<ArgumentNullException>(() => Maybe.Some(1).Where(null!)),
            () => Assert.Throws<ArgumentNullException>(() => Maybe.Some(1).Zip(Maybe.Some(2), (Func<int, int, int>)null!)),
            () => Assert.Throws<ArgumentNullException>(() => Maybe.Some(1).SelectMany(x => Maybe.Some(x), (Func<int, int, int>)null!)));
    }

    [Fact]
    internal void Select_with_a_nullable_value_type_turns_null_into_none_and_a_value_into_some()
    {
        var some = Maybe.Some(2).Select(static x => (int?)(x * 10));
        var none = Maybe.Some(2).Select(static _ => (int?)null);

        Assert.Multiple(
            () => Assert.Equal(20, some.GetValue()),
            () => Assert.True(none.IsNone));
    }

    [Fact]
    internal void A_query_that_selects_a_nullable_value_type_gives_a_maybe_of_the_value_type()
    {
        var some = from x in Maybe.Some(2) select (int?)(x + 1);
        var none = from x in Maybe.Some(2) select x > 5 ? (int?)x : null;

        Assert.Multiple(
            () => Assert.Equal(3, some.GetValue()),
            () => Assert.True(none.IsNone));
    }

    [Fact]
    internal void SelectMany_with_a_nullable_value_type_result_selector_turns_null_into_none()
    {
        var some = Maybe.Some(1).SelectMany(static x => Maybe.Some(x + 1), static (a, b) => (int?)(a + b));
        var none = Maybe.Some(1).SelectMany(static x => Maybe.Some(x + 1), static (_, _) => (int?)null);

        Assert.Multiple(
            () => Assert.Equal(3, some.GetValue()),
            () => Assert.True(none.IsNone));
    }

    [Fact]
    internal void SelectMany_with_only_a_selector_chains_like_bind()
    {
        var some = Maybe.Some(1).SelectMany(static x => Maybe.Some(x + 1));
        var noneSource = Maybe.None<int>().SelectMany(static x => Maybe.Some(x + 1));
        var noneInner = Maybe.Some(1).SelectMany(static _ => Maybe.None<int>());

        Assert.Multiple(
            () => Assert.Equal(2, some.GetValue()),
            () => Assert.True(noneSource.IsNone),
            () => Assert.True(noneInner.IsNone));
    }

    [Fact]
    internal void SelectMany_with_a_result_selector_returns_none_when_the_source_or_the_intermediate_maybe_has_no_value()
    {
        var noneSource = Maybe.None<int>().SelectMany(static x => Maybe.Some(x), static (a, b) => a + b);
        var noneInner = Maybe.Some(1).SelectMany(static _ => Maybe.None<int>(), static (a, b) => a + b);

        Assert.Multiple(
            () => Assert.True(noneSource.IsNone),
            () => Assert.True(noneInner.IsNone));
    }

    [Fact]
    internal void SelectMany_with_a_nullable_value_type_result_selector_returns_none_when_the_source_or_the_intermediate_maybe_has_no_value()
    {
        var calls = 0;

        int? CountedSum(int a, int b)
        {
            calls++;

            return a + b;
        }

        var noneSource = Maybe.None<int>().SelectMany(static x => Maybe.Some(x), CountedSum);
        var noneInner = Maybe.Some(1).SelectMany(static _ => Maybe.None<int>(), CountedSum);

        Assert.Multiple(
            () => Assert.True(noneSource.IsNone),
            () => Assert.True(noneInner.IsNone),
            () => Assert.Equal(0, calls));
    }
}
