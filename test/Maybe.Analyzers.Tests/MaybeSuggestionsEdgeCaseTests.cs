using Wyzely.Functional.MaybeAnalyzers;

namespace Wyzely.Functional.MaybeAnalyzersTests;

// The edges of the suggestion rules: what is written in another way (parentheses, null on the left, a member as the receiver), what only looks
// like the library, and diagnostics that no longer fit the code when a fix is asked for.
public sealed class MaybeSuggestionsEdgeCaseTests
{
    private const string Sources = """
        Maybe<int> Field = Maybe.Some(2);
        const int Limit = 10;
        sealed class Holder
        {
            public Holder Next() => this;
            public Maybe<int> Value => default;
        }
        sealed class Fake
        {
            public bool IsSome => true;
            public bool IsNone => false;
            public bool Flag => true;
            public int GetValue() => 1;
        }
        sealed class Factory
        {
            public Maybe<string> None() => default;
            public static Maybe<string> Make() => default;
            public static Maybe<T> None<T>() => default;
            public static Maybe<T> Some<T>(T value) => default;
        }
        """;

    private static readonly MaybeSuggestionsAnalyzer Analyzer = new();
    private static readonly MaybeSuggestionsCodeFixProvider Provider = new();

    [Theory]
    [InlineData("WYZ0203", "int M() => this.Field.IsSome ? this.Field.GetValue() : Limit;")]
    [InlineData("WYZ0203", "int M(Maybe<int> m) => (m.IsSome) ? m.GetValue() : 0;")]
    [InlineData("WYZ0204", "Maybe<string> M(string? s) => null == s ? Maybe.None<string>() : Maybe.Some(s);")]
    [InlineData("WYZ0204", "Maybe<string> M(string? s) => null != s ? Maybe.Some(s) : Maybe.None<string>();")]
    [InlineData("WYZ0204", "Maybe<string> M(string? s) => s is null ? Maybe.None<string>() : (Maybe.Some(s));")]
    [InlineData("WYZ0204", "Maybe<string> M(string? s) => (s is null) ? Maybe.None<string>() : Maybe.Some(s);")]
    internal async Task A_shape_written_with_a_member_parentheses_or_null_on_the_left_is_reported(string id, string method)
    {
        var ids = await AnalyzerTestHost.AnalyzeAsync(Sources + "\n" + method, Analyzer);

        Assert.Equal([id], ids);
    }

    [Theory]
    [InlineData("void M(Maybe<int> m) { if (m.IsSome) { Console.WriteLine(m); } }")]
    [InlineData("int M(Holder h) => h.Next().Value.IsSome ? h.Next().Value.GetValue() : 0;")]
    [InlineData("Maybe<int> M(int? n) => n is null ? Maybe.None<int>() : Maybe.Some(n.GetValueOrDefault());")]
    [InlineData("Maybe<T> M<T>(T value) where T : notnull => value is null ? Maybe.None<T>() : Maybe.Some(value);")]
    [InlineData("Maybe<string> M(string? s) => s is null ? Factory.None<string>() : Maybe.Some(s);")]
    [InlineData("Maybe<string> M(string? s) => s is null ? Maybe.None<string>() : Factory.Some(s);")]
    [InlineData("Maybe<string> M(string? s) => s is null ? Factory.Make() : Maybe.Some(s);")]
    [InlineData("Maybe<string> M(string? s, Factory factory) => s is null ? factory.None() : Maybe.Some(s);")]
    [InlineData("Maybe<string> M(string? s, bool flag) => flag ? Maybe.None<string>() : Maybe.Some(s!);")]
    [InlineData("Maybe<string> M(string? s) => s is not \"a\" ? Maybe.Some(s!) : Maybe.None<string>();")]
    [InlineData("Maybe<string> M(int n, string s) => n < 3 ? Maybe.None<string>() : Maybe.Some(s);")]
    [InlineData("int M(Fake f) => f.IsSome ? f.GetValue() : 0;")]
    [InlineData("string M() => nameof(Maybe<int>.IsSome);")]
    [InlineData("bool M(Maybe<int> m) => m.Filter<int>(x => x > 5).IsSome;")]
    [InlineData("int M(Fake f) => f.IsNone ? 0 : f.GetValue();")]
    [InlineData("int M(Fake f) => f.Flag ? 1 : 0;")]
    [InlineData("Maybe<string> M(string? s) => s is string ? Maybe.None<string>() : Maybe.Some(s!);")]
    [InlineData("Maybe<string> M(string? s) => s is not string ? Maybe.None<string>() : Maybe.Some(s);")]
    [InlineData("Maybe<string> M(string? s, string? t) => s == t ? Maybe.None<string>() : Maybe.Some(s!);")]
    internal async Task Shapes_that_only_look_like_a_suggestion_are_left_alone(string method)
    {
        var ids = await AnalyzerTestHost.AnalyzeAsync(Sources + "\n" + method, Analyzer);

        Assert.Empty(ids);
    }

    [Fact]
    internal async Task A_filter_of_another_type_is_not_the_filter_of_the_library()
    {
        var ids = await AnalyzerTestHost.AnalyzeSourceAsync(
            """
            static class Mine { public static Maybe<T> Filter<T>(this Maybe<T> maybe, Func<T, bool> predicate) => maybe; }
            public class Sample { bool M(Maybe<int> m) => m.Filter(x => x > 1).IsSome; }
            """,
            Analyzer);

        Assert.Empty(ids);
    }

    [Fact]
    internal async Task A_filter_in_a_class_of_the_same_name_in_another_namespace_is_not_the_filter_of_the_library()
    {
        var ids = await AnalyzerTestHost.AnalyzeSourceAsync(
            """
            static class MaybeExtensions { public static Maybe<T> Filter<T>(this Maybe<T> maybe, Func<T, bool> predicate) => maybe; }
            public class Sample { bool M(Maybe<int> m) => m.Filter(x => x > 1).IsSome; }
            """,
            Analyzer);

        Assert.Empty(ids);
    }

    [Theory]
    [InlineData("WYZ0203", "int M(Maybe<int> m) => (m.IsSome) ? m.GetValue() : 0;", "m.ValueOr(0)")]
    [InlineData("WYZ0203", "int M() => this.Field.IsSome ? this.Field.GetValue() : Limit;", "this.Field.ValueOr(Limit)")]
    [InlineData("WYZ0204", "Maybe<string> M(string? s) => null == s ? Maybe.None<string>() : Maybe.Some(s);", "Maybe.From(s)")]
    [InlineData("WYZ0204", "Maybe<string> M(string? s) => s is null ? Maybe.None<string>() : (Maybe.Some(s));", "Maybe.From(s)")]
    [InlineData("WYZ0204", "Maybe<string> M(string? s) => (s is null) ? Maybe.None<string>() : Maybe.Some(s);", "Maybe.From(s)")]
    internal async Task The_fix_handles_the_shape_written_with_a_member_parentheses_or_null_on_the_left(string id, string method, string expected)
    {
        var outcome = await CodeFixTestHost.FixAsync(Sources + "\n" + method, Provider, id, Analyzer);

        Assert.Multiple(
            () => Assert.NotNull(outcome.FixedSource),
            () => Assert.Contains(expected, outcome.FixedSource, StringComparison.Ordinal),
            () => Assert.DoesNotContain(id, outcome.RemainingIds));
    }

    // A diagnostic can be stale or can come from somewhere else, so the provider checks the code again and offers nothing that it cannot write.
    [Theory]
    [InlineData("WYZ0299", "int M(Maybe<int> m) => m.IsSome ? m.GetValue() : 0;", "m.IsSome ? m.GetValue() : 0")]
    [InlineData("WYZ0203", "int M(Maybe<int> m) => m.IsSome ? m.GetValue() : 0;", "int M")]
    [InlineData("WYZ0203", "int M(bool flag) => flag ? 1 : 2;", "flag ? 1 : 2")]
    [InlineData("WYZ0202", "bool M(Maybe<int> m) => m.IsSome;", "m.IsSome")]
    [InlineData(
        "WYZ0204",
        "Maybe<string> M(bool flag, string s) => flag ? Maybe.None<string>() : Maybe.Some(s);",
        "flag ? Maybe.None<string>() : Maybe.Some(s)")]
    [InlineData(
        "WYZ0204",
        "Maybe<string> M(string? s) => s is null ? Maybe.None<string>() : Maybe.None<string>();",
        "s is null ? Maybe.None<string>() : Maybe.None<string>()")]
    [InlineData(
        "WYZ0204",
        "Maybe<string> M(string? s, string t) => s == t ? Maybe.None<string>() : Maybe.Some(t);",
        "s == t ? Maybe.None<string>() : Maybe.Some(t)")]
    [InlineData(
        "WYZ0204",
        "Maybe<string> M(string? s) => s is null ? Maybe.None<string>() : Wrap(s); static Maybe<string> Wrap(string s) => default;",
        "s is null ? Maybe.None<string>() : Wrap(s)")]
    internal async Task No_fix_is_offered_for_a_diagnostic_that_does_not_fit_the_code(string id, string method, string marker)
    {
        var offered = await CodeFixTestHost.OfferedFixesAsync(method, Provider, id, marker);

        Assert.Equal(0, offered);
    }
}
