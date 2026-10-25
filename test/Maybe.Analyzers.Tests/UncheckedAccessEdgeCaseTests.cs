using Wyzely.Functional.MaybeAnalyzers;

namespace Wyzely.Functional.MaybeAnalyzersTests;

// The shapes around the guards of GetValue that the main tests do not reach: what cannot be analyzed, what only looks like a guard, and the
// forms of a check that are written with parentheses or with more than one operand.
public sealed class UncheckedAccessEdgeCaseTests
{
    [Fact]
    internal async Task A_conditional_access_cannot_be_analyzed_and_is_not_reported()
    {
        var ids = await AnalyzerTestHost.AnalyzeAsync("int? M(Maybe<int>? m) => m?.GetValue();");

        Assert.Empty(ids);
    }

    [Fact]
    internal async Task An_access_in_a_field_initializer_has_nothing_around_it_to_guard_it_and_is_reported()
    {
        var ids = await AnalyzerTestHost.AnalyzeAsync("static Maybe<int> Source; static int Value = Source.GetValue();");

        Assert.Equal(["WYZ0201"], ids);
    }

    [Fact]
    internal async Task An_early_exit_with_an_empty_block_does_not_guard_the_access()
    {
        var ids = await AnalyzerTestHost.AnalyzeAsync("int M(Maybe<int> m) { if (m.IsNone) { } return m.GetValue(); }");

        Assert.Equal(["WYZ0201"], ids);
    }

    [Fact]
    internal async Task A_check_in_parentheses_still_guards_the_access()
    {
        var ids = await AnalyzerTestHost.AnalyzeAsync("int M(Maybe<int> m) { if ((m.IsNone)) { return 0; } return m.GetValue(); }");

        Assert.Empty(ids);
    }

    [Theory]
    [InlineData("int M(Maybe<int> m, bool other) { if (other || m.IsNone) { return 0; } return m.GetValue(); }")]
    [InlineData("int M(Maybe<int> m, bool other) { if (m.IsNone || other) { return 0; } return m.GetValue(); }")]
    [InlineData("int M(Maybe<int> m, bool other) { if (m.IsSome && other) { return m.GetValue(); } return 0; }")]
    internal async Task A_check_that_is_one_operand_of_an_and_or_an_or_guards_the_access_on_either_side(string method)
    {
        var ids = await AnalyzerTestHost.AnalyzeAsync(method);

        Assert.Empty(ids);
    }

    [Theory]
    [InlineData("int M(Maybe<int> m, bool other) { if (m.IsSome && other) { return 1; } else { return m.GetValue(); } }")]
    [InlineData("int M(Maybe<int> m, bool other) { if (m.IsNone || other) { return m.GetValue(); } return 0; }")]
    [InlineData("int M(Maybe<int> m, Maybe<int> other) { if (m.Equals(other)) { return m.GetValue(); } return 0; }")]
    [InlineData("int M(Maybe<int> m, bool a, bool b) { if (a && b) { return m.GetValue(); } return 0; }")]
    [InlineData("int M(Maybe<int> m, bool a, bool b) { if (a || b) { return 0; } return m.GetValue(); }")]
    internal async Task A_check_that_says_nothing_about_the_value_in_the_branch_does_not_guard_the_access(string method)
    {
        var ids = await AnalyzerTestHost.AnalyzeAsync(method);

        Assert.Equal(["WYZ0201"], ids);
    }

    [Fact]
    internal async Task The_analyzers_do_nothing_for_a_compilation_that_does_not_know_the_library()
    {
        var usage = await AnalyzerTestHost.AnalyzeWithoutTheLibraryAsync(new MaybeUsageAnalyzer());
        var suggestions = await AnalyzerTestHost.AnalyzeWithoutTheLibraryAsync(new MaybeSuggestionsAnalyzer());

        Assert.Multiple(
            () => Assert.Empty(usage),
            () => Assert.Empty(suggestions));
    }
}
