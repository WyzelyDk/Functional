#if NET11_0_OR_GREATER

namespace Wyzely.Functional.MaybeTests;

// These tests need the C# 15 compiler and the .NET 11 runtime types, so they exist only for the net11.0 target. The exhaustive
// switch expressions below are part of what is tested: with warnings as errors, a maybe that was not recognised as a union
// (or that needed a discard arm) would not compile.
public sealed class MaybeUnionTests
{
    [Fact]
    internal void A_switch_matches_the_value_of_a_some()
    {
        Assert.Equal("some:2", Describe(Maybe.Some(2)));
    }

    [Fact]
    internal void A_switch_matches_none_with_the_null_case()
    {
        Assert.Equal("none", Describe(Maybe.None<int>()));
    }

    [Fact]
    internal void A_default_maybe_matches_the_null_case()
    {
        Assert.Equal("none", Describe(default));
    }

    [Fact]
    internal void A_reference_value_is_matched_by_its_type()
    {
        Assert.Equal("Ada", DescribeText(Maybe.Some("Ada")));
    }

    [Fact]
    internal void The_language_converts_a_case_type_to_the_union_implicitly()
    {
        Maybe<int> fromValue = 5;

        Assert.Equal(5, fromValue.GetValue());
    }

    [Fact]
    internal void The_union_members_expose_the_contained_case()
    {
        var some = (Maybe<int>.IUnionMembers)Maybe.Some(2);
        var none = (Maybe<int>.IUnionMembers)Maybe.None<int>();

        Assert.Multiple(
            () => Assert.Equal(2, some.Value),
            () => Assert.True(some.HasValue),
            () => Assert.Null(none.Value),
            () => Assert.False(none.HasValue));
    }

    [Fact]
    internal void The_non_boxing_access_reports_the_value_of_a_some_only()
    {
        var some = (Maybe<int>.IUnionMembers)Maybe.Some(2);
        var none = (Maybe<int>.IUnionMembers)Maybe.None<int>();

        var someHasValue = some.TryGetValue(out var value);
        var noneHasValue = none.TryGetValue(out _);

        Assert.Multiple(
            () => Assert.True(someHasValue),
            () => Assert.Equal(2, value),
            () => Assert.False(noneHasValue));
    }

    [Fact]
    internal void The_union_member_Create_makes_a_some_from_a_value_and_none_from_null()
    {
        Assert.Multiple(
            () => Assert.Equal(5, Maybe<int>.IUnionMembers.Create(5).GetValue()),
            () => Assert.True(Maybe<string>.IUnionMembers.Create(null!).IsNone));
    }

    private static string Describe(Maybe<int> maybe) => maybe switch
    {
        int value => $"some:{value}",
        null => "none",
    };

    private static string DescribeText(Maybe<string> maybe) => maybe switch
    {
        string text => text,
        null => "none",
    };
}

#endif
