namespace Wyzely.Functional.MaybeTests;

// The values of a maybe are compared with the default equality and order of their type. A comparer that is given decides instead,
// for one comparison (Equals, GetHashCode, CompareTo, Contains) or for a collection of maybes (CreateEqualityComparer, CreateComparer).
public sealed class MaybeComparerTests
{
    private static readonly IEqualityComparer<string> IgnoreCase = StringComparer.OrdinalIgnoreCase;
    private static readonly IEqualityComparer<string> NeverAsked = EqualityComparer<string>.Create(
        static (_, _) => throw new InvalidOperationException("The comparer must not be asked."),
        static _ => throw new InvalidOperationException("The comparer must not be asked."));

    [Fact]
    internal void Equals_with_a_comparer_finds_two_values_equal_that_the_comparer_finds_equal()
    {
        var upper = Maybe.Some("A");
        var lower = Maybe.Some("a");

        Assert.Multiple(
            () => Assert.True(lower.Equals(upper, IgnoreCase)),
            () => Assert.False(lower.Equals(upper)));
    }

    [Fact]
    internal void Equals_with_a_comparer_finds_two_values_different_that_the_comparer_finds_different()
    {
        var left = Maybe.Some("a");
        var right = Maybe.Some("b");

        Assert.False(left.Equals(right, IgnoreCase));
    }

    [Fact]
    internal void Equals_with_a_comparer_is_true_for_two_nones_without_asking_the_comparer()
    {
        var none = Maybe.None<string>();

        Assert.True(none.Equals(default, NeverAsked));
    }

    [Fact]
    internal void Equals_with_a_comparer_is_false_for_a_some_and_a_none_without_asking_the_comparer()
    {
        var some = Maybe.Some("a");
        var none = Maybe.None<string>();

        Assert.Multiple(
            () => Assert.False(some.Equals(none, NeverAsked)),
            () => Assert.False(none.Equals(some, NeverAsked)));
    }

    [Fact]
    internal void Equals_with_a_comparer_hands_the_two_values_to_the_comparer_in_order()
    {
        var seen = new List<(string Left, string Right)>();
        var recording = EqualityComparer<string>.Create((left, right) =>
        {
            seen.Add((left!, right!));

            return true;
        });

        var outcome = Maybe.Some("first").Equals(Maybe.Some("second"), recording);

        Assert.Multiple(
            () => Assert.True(outcome),
            () => Assert.Equal([("first", "second")], seen));
    }

    [Fact]
    internal void GetHashCode_with_a_comparer_is_the_same_for_values_the_comparer_finds_equal()
    {
        var upper = Maybe.Some("A");
        var lower = Maybe.Some("a");

        Assert.Equal(upper.GetHashCode(IgnoreCase), lower.GetHashCode(IgnoreCase));
    }

    [Fact]
    internal void GetHashCode_with_a_comparer_uses_the_hash_of_the_comparer()
    {
        var constant = EqualityComparer<string>.Create(static (left, right) => left == right, static _ => 7);

        var first = Maybe.Some("a").GetHashCode(constant);
        var second = Maybe.Some("b").GetHashCode(constant);

        Assert.Multiple(
            () => Assert.Equal(first, second),
            () => Assert.NotEqual(Maybe.Some("a").GetHashCode(), Maybe.Some("b").GetHashCode()));
    }

    [Fact]
    internal void GetHashCode_with_a_comparer_of_none_is_the_hash_of_none_without_asking_the_comparer()
    {
        var none = Maybe.None<string>();

        Assert.Equal(none.GetHashCode(), none.GetHashCode(NeverAsked));
    }

    [Fact]
    internal void CompareTo_with_a_comparer_orders_the_values_by_the_comparer()
    {
        var descending = Comparer<int>.Create(static (left, right) => right.CompareTo(left));
        var one = Maybe.Some(1);
        var two = Maybe.Some(2);

        Assert.Multiple(
            () => Assert.True(one.CompareTo(two, descending) > 0),
            () => Assert.True(two.CompareTo(one, descending) < 0),
            () => Assert.Equal(0, one.CompareTo(one, descending)),
            () => Assert.True(one.CompareTo(two) < 0));
    }

    [Fact]
    internal void CompareTo_with_a_comparer_sorts_none_before_some_without_asking_the_comparer()
    {
        var thrower = Comparer<int>.Create(static (_, _) => throw new InvalidOperationException("The comparer must not be asked."));
        var none = Maybe.None<int>();
        var some = Maybe.Some(1);

        Assert.Multiple(
            () => Assert.True(none.CompareTo(some, thrower) < 0),
            () => Assert.True(some.CompareTo(none, thrower) > 0),
            () => Assert.Equal(0, none.CompareTo(none, thrower)));
    }

    [Fact]
    internal void CreateEqualityComparer_makes_a_hash_set_treat_values_the_comparer_finds_equal_as_one()
    {
        var set = new HashSet<Maybe<string>>(Maybe.CreateEqualityComparer(IgnoreCase))
        {
            Maybe.Some("A"),
            Maybe.Some("a"),
            Maybe.None<string>(),
        };

        Assert.Multiple(
            () => Assert.Equal(2, set.Count),
            () => Assert.Contains(Maybe.Some("a"), set),
            () => Assert.Contains(default, set));
    }

    [Fact]
    internal void CreateEqualityComparer_makes_a_dictionary_find_a_key_the_comparer_finds_equal()
    {
        var prices = new Dictionary<Maybe<string>, int>(Maybe.CreateEqualityComparer(IgnoreCase))
        {
            [Maybe.Some("Tea")] = 5,
        };

        Assert.Equal(5, prices[Maybe.Some("TEA")]);
    }

    [Fact]
    internal void CreateEqualityComparer_makes_distinct_drop_the_values_the_comparer_finds_equal()
    {
        Maybe<string>[] maybes = [Maybe.Some("a"), Maybe.Some("A"), Maybe.None<string>(), Maybe.Some("b"), default];

        var distinct = maybes.Distinct(Maybe.CreateEqualityComparer(IgnoreCase)).ToList();

        Assert.Equal(3, distinct.Count);
    }

    [Fact]
    internal void CreateEqualityComparer_separates_none_from_some()
    {
        var comparer = Maybe.CreateEqualityComparer(IgnoreCase);

        Assert.Multiple(
            () => Assert.True(comparer.Equals(default, Maybe.None<string>())),
            () => Assert.False(comparer.Equals(default, Maybe.Some("a"))),
            () => Assert.Equal(comparer.GetHashCode(default), comparer.GetHashCode(Maybe.None<string>())));
    }

    [Fact]
    internal void CreateComparer_sorts_none_first_and_the_values_by_the_comparer()
    {
        var descending = Comparer<int>.Create(static (left, right) => right.CompareTo(left));
        Maybe<int>[] maybes = [Maybe.Some(1), default, Maybe.Some(3), Maybe.Some(2)];

        var sorted = maybes.Order(Maybe.CreateComparer(descending)).ToList();

        Assert.Equal([default, Maybe.Some(3), Maybe.Some(2), Maybe.Some(1)], sorted);
    }
}
