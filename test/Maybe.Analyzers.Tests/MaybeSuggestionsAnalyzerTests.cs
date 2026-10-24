using Wyzely.Functional.MaybeAnalyzers;

namespace Wyzely.Functional.MaybeAnalyzersTests;

// The suggestion rules recognise exact shapes only: a snippet that looks similar but would change the behavior or the type must stay silent.
public sealed class MaybeSuggestionsAnalyzerTests
{
    private const string Sources = """
        static Maybe<int> Find() => Maybe.Some(1);
        static int Compute() => 42;
        static bool Big(int x) => x > 5;
        static string? Text() => null;
        const int Limit = 10;
        Maybe<int> Field = Maybe.Some(2);
        """;

    private static readonly MaybeSuggestionsAnalyzer Analyzer = new();

    [Theory]
    [InlineData("WYZ0202", "bool M(Maybe<int> m) => m.Filter(x => x > 5).IsSome;")]
    [InlineData("WYZ0202", "bool M(Maybe<int> m) => m.Filter(Big).IsSome;")]
    [InlineData("WYZ0202", "bool M() => Find().Filter(x => x > 5).IsSome;")]
    [InlineData("WYZ0203", "int M(Maybe<int> m) => m.IsSome ? m.GetValue() : 0;")]
    [InlineData("WYZ0203", "int M(Maybe<int> m) => m.IsNone ? 0 : m.GetValue();")]
    [InlineData("WYZ0203", "int M(Maybe<int> m, int fallback) => m.IsSome ? m.GetValue() : fallback;")]
    [InlineData("WYZ0203", "int M() => Field.IsSome ? Field.GetValue() : Limit;")]
    [InlineData("WYZ0203", "string M(Maybe<string> m) => m.IsSome ? m.GetValue() : \"none\";")]
    [InlineData("WYZ0204", "Maybe<string> M(string? s) => s is null ? Maybe.None<string>() : Maybe.Some(s);")]
    [InlineData("WYZ0204", "Maybe<string> M(string? s) => s == null ? Maybe.None<string>() : Maybe.Some(s);")]
    [InlineData("WYZ0204", "Maybe<string> M(string? s) => s is not null ? Maybe.Some(s) : Maybe.None<string>();")]
    [InlineData("WYZ0204", "Maybe<string> M(string? s) => s != null ? Maybe.Some(s) : default;")]
    [InlineData("WYZ0204", "Maybe<int> M(int? n) => n is null ? Maybe.None<int>() : Maybe.Some(n.Value);")]
    internal async Task A_shape_with_a_simpler_form_is_reported(string id, string method)
    {
        var ids = await AnalyzerTestHost.AnalyzeAsync(Sources + "\n" + method, Analyzer);

        Assert.Equal([id], ids);
    }

    // Each of these looks like one of the shapes above and would change the result, the type or what is evaluated when it is rewritten.
    [Theory]
    [InlineData("bool M(Maybe<int> m) => m.Filter(x => x > 5).IsNone;")]
    [InlineData("bool M(Maybe<int> m) => m.IsSome;")]
    [InlineData("Maybe<int> M(Maybe<int> m) => m.Filter(x => x > 5);")]
    [InlineData("int M(Maybe<int> m) => m.IsSome ? m.GetValue() : Compute();")]
    [InlineData("int M(Maybe<int> m) => m.IsSome ? m.GetValue() + 1 : 0;")]
    [InlineData("int M(Maybe<int> m, Maybe<int> other) => m.IsSome ? other.GetValue() : 0;")]
    [InlineData("int M() => Find().IsSome ? Find().GetValue() : 0;")]
    [InlineData("int M(Maybe<int> m) => m.IsSome ? 1 : 0;")]
    [InlineData("int M(Maybe<int> m) => m.IsSome ? 0 : m.GetValue();")]
    [InlineData("int M(Maybe<int> m) => m.IsNone ? m.GetValue() : 0;")]
    [InlineData("Maybe<string> M(string? s) => s is null ? Maybe.None<string>() : Maybe.Some(s.Trim());")]
    [InlineData("Maybe<string> M(string? s) => s is null ? Maybe.Some(\"x\") : Maybe.Some(s);")]
    [InlineData("Maybe<string> M(string? s, string? t) => s is null ? Maybe.None<string>() : Maybe.Some(t!);")]
    [InlineData("Maybe<string> M(string? s) => s is null ? Maybe.None<string>() : Maybe.None<string>();")]
    [InlineData("Maybe<string> M(string? s) => s is \"a\" ? Maybe.None<string>() : Maybe.Some(s!);")]
    [InlineData("Maybe<string> M() => Text() is null ? Maybe.None<string>() : Maybe.Some(Text()!);")]
    internal async Task Shapes_that_would_change_the_behavior_or_do_not_match_are_left_alone(string method)
    {
        var ids = await AnalyzerTestHost.AnalyzeAsync(Sources + "\n" + method, Analyzer);

        Assert.Empty(ids);
    }

    [Fact]
    internal void Every_rule_is_an_info_suggestion_in_the_design_category_with_a_help_link_to_its_page()
    {
        Assert.Multiple(
            () => Assert.Equal(["WYZ0202", "WYZ0203", "WYZ0204"], Analyzer.SupportedDiagnostics.Select(descriptor => descriptor.Id).Order()),
            () => Assert.All(
                Analyzer.SupportedDiagnostics,
                descriptor => Assert.Equal(Microsoft.CodeAnalysis.DiagnosticSeverity.Info, descriptor.DefaultSeverity)),
            () => Assert.All(Analyzer.SupportedDiagnostics, descriptor => Assert.Equal("Design", descriptor.Category)),
            () => Assert.All(
                Analyzer.SupportedDiagnostics,
                descriptor => Assert.EndsWith("/docs/maybe/analyzers/" + descriptor.Id + ".md", descriptor.HelpLinkUri, StringComparison.Ordinal)));
    }
}
