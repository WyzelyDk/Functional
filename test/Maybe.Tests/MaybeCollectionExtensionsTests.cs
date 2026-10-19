namespace Wyzely.Functional.MaybeTests;

public sealed class MaybeCollectionExtensionsTests
{
    [Fact]
    internal void FirstOrNone_returns_the_first_element_or_none_for_an_empty_sequence()
    {
        Assert.Multiple(
            () => Assert.Equal(1, new[] { 1, 2 }.FirstOrNone().GetValue()),
            () => Assert.True(Array.Empty<int>().FirstOrNone().IsNone));
    }

    [Fact]
    internal void FirstOrNone_with_a_predicate_returns_the_first_match_or_none()
    {
        Assert.Multiple(
            () => Assert.Equal(2, new[] { 1, 2, 4 }.FirstOrNone(x => x % 2 == 0).GetValue()),
            () => Assert.True(new[] { 1, 3 }.FirstOrNone(x => x % 2 == 0).IsNone));
    }

    [Fact]
    internal void FirstOrNone_is_none_when_the_first_element_is_null()
    {
        var names = new[] { null!, "a" };

        Assert.True(names.FirstOrNone().IsNone);
    }

    [Fact]
    internal void LastOrNone_returns_the_last_element_or_none()
    {
        Assert.Multiple(
            () => Assert.Equal(2, new[] { 1, 2 }.LastOrNone().GetValue()),
            () => Assert.True(Array.Empty<int>().LastOrNone().IsNone),
            () => Assert.Equal(4, new[] { 1, 2, 4, 5 }.LastOrNone(x => x % 2 == 0).GetValue()),
            () => Assert.True(new[] { 1, 3 }.LastOrNone(x => x % 2 == 0).IsNone));
    }

    [Fact]
    internal void SingleOrNone_returns_the_only_element_and_none_for_zero_or_many()
    {
        Assert.Multiple(
            () => Assert.Equal(5, new[] { 5 }.SingleOrNone().GetValue()),
            () => Assert.True(Array.Empty<int>().SingleOrNone().IsNone),
            () => Assert.True(new[] { 1, 2 }.SingleOrNone().IsNone),
            () => Assert.Equal(2, new[] { 1, 2, 3 }.SingleOrNone(x => x == 2).GetValue()),
            () => Assert.True(new[] { 2, 4 }.SingleOrNone(x => x % 2 == 0).IsNone));
    }

    [Fact]
    internal void ElementAtOrNone_returns_the_element_or_none_when_out_of_range()
    {
        var items = new[] { 10, 20 };

        Assert.Multiple(
            () => Assert.Equal(20, items.ElementAtOrNone(1).GetValue()),
            () => Assert.True(items.ElementAtOrNone(2).IsNone),
            () => Assert.True(items.ElementAtOrNone(-1).IsNone));
    }

    [Fact]
    internal void GetValueOrNone_looks_a_key_up_in_a_dictionary()
    {
        var map = new Dictionary<string, int> { ["a"] = 1 };
        IReadOnlyDictionary<string, int> readOnly = map;

        Assert.Multiple(
            () => Assert.Equal(1, map.GetValueOrNone("a").GetValue()),
            () => Assert.True(map.GetValueOrNone("b").IsNone),
            () => Assert.Equal(1, readOnly.GetValueOrNone("a").GetValue()),
            () => Assert.True(readOnly.GetValueOrNone("b").IsNone));
    }

    [Fact]
    internal void Choose_keeps_only_the_values_of_the_somes_in_order()
    {
        var maybes = new[] { Maybe.Some(1), Maybe.None<int>(), Maybe.Some(3) };

        Assert.Equal([1, 3], maybes.Choose());
    }

    [Fact]
    internal void Choose_with_a_selector_maps_and_drops_the_nones()
    {
        var values = new[] { "1", "x", "3" };

        var numbers = values.Choose(text => int.TryParse(text, out var number) ? Maybe.Some(number) : Maybe.None<int>());

        Assert.Equal([1, 3], numbers);
    }

    [Fact]
    internal void The_methods_throw_when_the_source_or_a_delegate_is_null()
    {
        Assert.Multiple(
            () => Assert.Throws<ArgumentNullException>(() => ((IEnumerable<int>)null!).FirstOrNone()),
            () => Assert.Throws<ArgumentNullException>(() => new[] { 1 }.FirstOrNone(null!)),
            () => Assert.Throws<ArgumentNullException>(() => ((IEnumerable<Maybe<int>>)null!).Choose()),
            () => Assert.Throws<ArgumentNullException>(() => new[] { 1 }.Choose((Func<int, Maybe<int>>)null!)),
            () => Assert.Throws<ArgumentNullException>(() => ((IReadOnlyDictionary<string, int>)null!).GetValueOrNone("a")));
    }

    [Fact]
    internal void LastOrNone_returns_the_last_element_or_none_for_a_sequence_that_is_not_a_list()
    {
        Assert.Multiple(
            () => Assert.Equal(3, Lazily(1, 2, 3).LastOrNone().GetValue()),
            () => Assert.True(Lazily().LastOrNone().IsNone));
    }

    [Fact]
    internal void ElementAtOrNone_returns_the_element_or_none_for_a_sequence_that_is_not_a_list()
    {
        Assert.Multiple(
            () => Assert.Equal(20, Lazily(10, 20).ElementAtOrNone(1).GetValue()),
            () => Assert.True(Lazily(10, 20).ElementAtOrNone(2).IsNone),
            () => Assert.True(Lazily(10, 20).ElementAtOrNone(-1).IsNone));
    }

    private static IEnumerable<int> Lazily(params int[] items)
    {
        foreach (var item in items)
        {
            yield return item;
        }
    }
}
