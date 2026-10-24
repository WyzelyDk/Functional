using Wyzely.Functional.MaybeAnalyzers;

namespace Wyzely.Functional.MaybeAnalyzersTests;

// A suggestion is only as good as its fix: the rewritten code must say the same thing, and the diagnostic must be gone afterwards.
public sealed class MaybeSuggestionsCodeFixTests
{
    private const string Sources = """
        static Maybe<int> Find() => Maybe.Some(1);
        const int Limit = 10;
        Maybe<int> Field = Maybe.Some(2);
        """;

    private static readonly MaybeSuggestionsCodeFixProvider Provider = new();
    private static readonly MaybeSuggestionsAnalyzer Analyzer = new();

    [Theory]
    [InlineData("WYZ0202", "bool M(Maybe<int> m) => m.Filter(x => x > 5).IsSome;", "m.Any(x => x > 5)")]
    [InlineData("WYZ0202", "bool M() => Find().Filter(x => x > 5).IsSome;", "Find().Any(x => x > 5)")]
    [InlineData("WYZ0203", "int M(Maybe<int> m) => m.IsSome ? m.GetValue() : 0;", "m.ValueOr(0)")]
    [InlineData("WYZ0203", "int M(Maybe<int> m) => m.IsNone ? 0 : m.GetValue();", "m.ValueOr(0)")]
    [InlineData("WYZ0203", "int M(Maybe<int> m, int fallback) => m.IsSome ? m.GetValue() : fallback;", "m.ValueOr(fallback)")]
    [InlineData("WYZ0203", "int M() => Field.IsSome ? Field.GetValue() : Limit;", "Field.ValueOr(Limit)")]
    [InlineData(
        "WYZ0204",
        "Maybe<string> M(string? s) => s is null ? Maybe.None<string>() : Maybe.Some(s);",
        "Maybe.From(s)")]
    [InlineData(
        "WYZ0204",
        "Maybe<string> M(string? s) => s != null ? Maybe.Some(s) : default;",
        "Maybe.From(s)")]
    [InlineData(
        "WYZ0204",
        "Maybe<int> M(int? n) => n is null ? Maybe.None<int>() : Maybe.Some(n.Value);",
        "Maybe.From(n)")]
    internal async Task The_fix_rewrites_the_code_and_the_suggestion_is_gone(string id, string method, string expected)
    {
        var outcome = await CodeFixTestHost.FixAsync(Sources + "\n" + method, Provider, id, Analyzer);

        Assert.Multiple(
            () => Assert.NotNull(outcome.FixedSource),
            () => Assert.Contains(expected, outcome.FixedSource, StringComparison.Ordinal),
            () => Assert.DoesNotContain(id, outcome.RemainingIds));
    }

    [Fact]
    internal void The_provider_fixes_the_three_suggestions_and_supports_fixing_all_occurrences()
    {
        Assert.Multiple(
            () => Assert.Equal(["WYZ0202", "WYZ0203", "WYZ0204"], Provider.FixableDiagnosticIds),
            () => Assert.NotNull(Provider.GetFixAllProvider()));
    }
}
