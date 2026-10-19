namespace Wyzely.Functional.MaybeTests;

public sealed class MaybeOfTTests
{
    [Fact]
    internal void A_default_maybe_is_none()
    {
        var maybe = default(Maybe<int>);

        Assert.Multiple(
            () => Assert.True(maybe.IsNone),
            () => Assert.False(maybe.IsSome));
    }

    [Fact]
    internal void Some_holds_the_value()
    {
        var maybe = Maybe.Some(42);

        Assert.Multiple(
            () => Assert.True(maybe.IsSome),
            () => Assert.False(maybe.IsNone),
            () => Assert.Equal(42, maybe.GetValue()));
    }

    [Fact]
    internal void Some_throws_when_the_value_is_null()
    {
        Assert.Throws<ArgumentNullException>(() => Maybe.Some<string>(null!));
    }

    [Fact]
    internal void None_has_no_value()
    {
        Assert.True(Maybe.None<string>().IsNone);
    }

    [Fact]
    internal void From_a_reference_is_none_when_the_reference_is_null()
    {
        Assert.Multiple(
            () => Assert.True(Maybe.From((string?)null).IsNone),
            () => Assert.Equal("a", Maybe.From("a").GetValue()));
    }

    [Fact]
    internal void From_a_nullable_value_is_none_when_it_has_no_value()
    {
        Assert.Multiple(
            () => Assert.True(Maybe.From((int?)null).IsNone),
            () => Assert.Equal(7, Maybe.From((int?)7).GetValue()));
    }

    [Fact]
    internal void SomeWhen_is_some_only_when_the_condition_is_true()
    {
        Assert.Multiple(
            () => Assert.Equal(5, Maybe.SomeWhen(true, 5).GetValue()),
            () => Assert.True(Maybe.SomeWhen(false, 5).IsNone));
    }

    [Fact]
    internal void NoneWhen_is_none_only_when_the_condition_is_true()
    {
        Assert.Multiple(
            () => Assert.True(Maybe.NoneWhen(true, 5).IsNone),
            () => Assert.Equal(5, Maybe.NoneWhen(false, 5).GetValue()));
    }

    [Fact]
    internal void A_value_converts_implicitly_to_some()
    {
        Maybe<int> maybe = 3;

        Assert.Equal(3, maybe.GetValue());
    }

    [Fact]
    internal void A_null_reference_converts_implicitly_to_none()
    {
        Maybe<string> maybe = (string?)null;

        Assert.True(maybe.IsNone);
    }

    [Fact]
    internal void GetValue_throws_with_a_helpful_message_when_there_is_no_value()
    {
        var exception = Assert.Throws<InvalidOperationException>(() => Maybe.None<int>().GetValue());

        Assert.Equal("The maybe has no value. Check IsSome first, or use TryGetValue.", exception.Message);
    }

    [Fact]
    internal void TryGetValue_returns_the_value_and_true_when_there_is_one()
    {
        var found = Maybe.Some("x").TryGetValue(out var value);

        Assert.Multiple(
            () => Assert.True(found),
            () => Assert.Equal("x", value));
    }

    [Fact]
    internal void TryGetValue_returns_false_when_there_is_no_value()
    {
        var found = Maybe.None<string>().TryGetValue(out var value);

        Assert.Multiple(
            () => Assert.False(found),
            () => Assert.Null(value));
    }

    [Fact]
    internal void ValueOr_returns_the_value_when_there_is_one_and_the_fallback_otherwise()
    {
        Assert.Multiple(
            () => Assert.Equal(1, Maybe.Some(1).ValueOr(9)),
            () => Assert.Equal(9, Maybe.None<int>().ValueOr(9)));
    }

    [Fact]
    internal void ToEnumerable_yields_the_value_or_nothing()
    {
        Assert.Multiple(
            () => Assert.Equal([4], Maybe.Some(4).ToEnumerable()),
            () => Assert.Empty(Maybe.None<int>().ToEnumerable()));
    }

    [Fact]
    internal void ToNullable_gives_the_value_or_null_for_a_value_type()
    {
        Assert.Multiple(
            () => Assert.Equal(4, Maybe.Some(4).ToNullable()),
            () => Assert.Null(Maybe.None<int>().ToNullable()));
    }

    [Fact]
    internal void Two_somes_with_equal_values_are_equal()
    {
        Assert.Multiple(
            () => Assert.True(Maybe.Some(1) == Maybe.Some(1)),
            () => Assert.True(Maybe.Some(1).Equals((object)Maybe.Some(1))),
            () => Assert.Equal(Maybe.Some(1).GetHashCode(), Maybe.Some(1).GetHashCode()));
    }

    [Fact]
    internal void Somes_with_different_values_and_a_some_and_a_none_are_different()
    {
        Assert.Multiple(
            () => Assert.True(Maybe.Some(1) != Maybe.Some(2)),
            () => Assert.True(Maybe.Some(1) != Maybe.None<int>()),
            () => Assert.False(Maybe.Some(0).Equals(default)));
    }

    [Fact]
    internal void Two_nones_are_equal_and_have_the_same_hash_code()
    {
        Assert.Multiple(
            () => Assert.True(Maybe.None<string>() == default),
            () => Assert.Equal(Maybe.None<string>().GetHashCode(), default(Maybe<string>).GetHashCode()));
    }

    [Fact]
    internal void None_sorts_before_some_and_somes_sort_by_their_values()
    {
        var sorted = new[] { Maybe.Some(3), Maybe.None<int>(), Maybe.Some(1) }.Order().ToArray();

        Assert.Equal([Maybe.None<int>(), Maybe.Some(1), Maybe.Some(3)], sorted);
    }

    [Fact]
    internal void ToString_describes_some_and_none()
    {
        Assert.Multiple(
            () => Assert.Equal("Some(42)", Maybe.Some(42).ToString()),
            () => Assert.Equal("None", Maybe.None<int>().ToString()));
    }

    [Fact]
    internal void A_maybe_is_not_equal_to_an_object_of_another_type_or_to_null()
    {
        Assert.Multiple(
            () => Assert.False(Maybe.Some(1).Equals("1")),
            () => Assert.False(Maybe.Some(1).Equals(null)));
    }

    [Fact]
    internal void ToString_shows_nothing_for_a_value_that_has_no_text()
    {
        Assert.Equal("Some()", Maybe.Some(new WithoutText()).ToString());
    }

    private sealed class WithoutText
    {
        public override string? ToString() => null;
    }
}
