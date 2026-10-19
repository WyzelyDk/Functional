namespace Wyzely.Functional.MaybeAnalyzersTests;

// The nullable annotations are part of the API: these tests compile consumer code with nullable reference types enabled and
// check which warnings the compiler reports, in both directions (what is accepted and what is rejected).
public sealed class ConsumerNullabilityTests
{
    [Fact]
    internal void TryGetValue_makes_the_value_non_null_when_it_returns_true()
    {
        var warnings = AnalyzerTestHost.NullabilityWarnings("int M(Maybe<string> m) { if (m.TryGetValue(out var v)) { return v.Length; } return 0; }");

        Assert.Empty(warnings);
    }

    [Fact]
    internal void TryGetValue_leaves_the_value_maybe_null_when_it_returns_false()
    {
        var warnings = AnalyzerTestHost.NullabilityWarnings("int M(Maybe<string> m) { if (!m.TryGetValue(out var v)) { return v.Length; } return 0; }");

        Assert.Equal(["CS8602"], warnings);
    }

    [Fact]
    internal void A_nullable_reference_type_argument_is_reported_against_the_notnull_constraint()
    {
        var warnings = AnalyzerTestHost.NullabilityWarnings("Maybe<string?> M() => default;");

        Assert.Equal(["CS8714"], warnings);
    }

    [Fact]
    internal void A_nullable_reference_converts_to_a_maybe_without_a_warning()
    {
        var warnings = AnalyzerTestHost.NullabilityWarnings("Maybe<string> M(string? text) => text;");

        Assert.Empty(warnings);
    }

    [Fact]
    internal void A_null_literal_converts_to_a_maybe_without_a_warning()
    {
        var warnings = AnalyzerTestHost.NullabilityWarnings("Maybe<string> M() => null;");

        Assert.Empty(warnings);
    }

    [Fact]
    internal void Some_rejects_a_null_literal()
    {
        var warnings = AnalyzerTestHost.NullabilityWarnings("Maybe<string> M() => Maybe.Some<string>(null);");

        Assert.Equal(["CS8625"], warnings);
    }

    [Fact]
    internal void None_is_available_for_a_non_nullable_type()
    {
        var warnings = AnalyzerTestHost.NullabilityWarnings("Maybe<string> M() => Maybe.None<string>();");

        Assert.Empty(warnings);
    }

    [Fact]
    internal void ValueOr_with_a_non_null_fallback_is_non_null()
    {
        var warnings = AnalyzerTestHost.NullabilityWarnings("int M(Maybe<string> m) => m.ValueOr(\"none\").Length;");

        Assert.Empty(warnings);
    }

    [Fact]
    internal void ValueOr_rejects_a_null_fallback()
    {
        var warnings = AnalyzerTestHost.NullabilityWarnings("string M(Maybe<string> m) => m.ValueOr(null);");

        Assert.Equal(["CS8625"], warnings);
    }

    [Fact]
    internal void The_value_passed_to_a_delegate_is_non_null()
    {
        var warnings = AnalyzerTestHost.NullabilityWarnings("Maybe<int> M(Maybe<string> m) => m.Map(v => v.Length);");

        Assert.Empty(warnings);
    }

    [Fact]
    internal void The_value_passed_to_Match_is_non_null()
    {
        var warnings = AnalyzerTestHost.NullabilityWarnings("int M(Maybe<string> m) => m.Match(v => v.Length, () => 0);");

        Assert.Empty(warnings);
    }

    [Fact]
    internal void Map_accepts_a_delegate_that_returns_a_nullable_reference_and_turns_null_into_none()
    {
        var warnings = AnalyzerTestHost.NullabilityWarnings("Maybe<string> M(Maybe<string> m) => m.Map(v => (string?)null);");

        Assert.Empty(warnings);
    }

    [Fact]
    internal void Map_to_a_nullable_reference_infers_the_non_nullable_type_with_var()
    {
        var warnings = AnalyzerTestHost.NullabilityWarnings("string M(Maybe<string> m) { var mapped = m.Map(v => (string?)null); return " +
            "mapped.ValueOr(\"none\"); }");

        Assert.Empty(warnings);
    }

    [Fact]
    internal void Map_accepts_an_asynchronous_delegate_that_returns_a_nullable_reference()
    {
        var warnings = AnalyzerTestHost.NullabilityWarnings("Task<Maybe<string>> M(Maybe<string> m) => m.Map(v => Task.FromResult((string?)null));");

        Assert.Empty(warnings);
    }

    [Fact]
    internal void An_async_receiver_keeps_the_nullability_of_its_value()
    {
        var warnings = AnalyzerTestHost.NullabilityWarnings("Task<Maybe<int>> M(Task<Maybe<string>> m) => m.Map(v => v.Length);");

        Assert.Empty(warnings);
    }

    [Fact]
    internal void Map_to_a_nullable_value_type_gives_a_maybe_of_the_value_type_without_a_warning()
    {
        var warnings = AnalyzerTestHost.NullabilityWarnings("Maybe<int> M(Maybe<string> m) => m.Map(v => (int?)v.Length);");

        Assert.Empty(warnings);
    }

    [Fact]
    internal void Map_to_a_nullable_value_type_infers_the_value_type_with_var()
    {
        var warnings = AnalyzerTestHost.NullabilityWarnings("int M(Maybe<string> m) { var mapped = m.Map(v => (int?)v.Length); return mapped.ValueOr(0); }");

        Assert.Empty(warnings);
    }

    [Fact]
    internal void Map_to_a_property_that_is_a_nullable_value_type_gives_a_maybe_of_the_value_type()
    {
        var warnings = AnalyzerTestHost.NullabilityWarnings("class P { public int? Age { get; set; } } Maybe<int> M(Maybe<P> m) => m.Map(p => p.Age);");

        Assert.Empty(warnings);
    }

    [Fact]
    internal void Map_to_a_property_that_is_a_nullable_reference_gives_a_maybe_of_the_reference_type()
    {
        var warnings = AnalyzerTestHost.NullabilityWarnings("class P { public string? Nick { get; set; } } Maybe<string> M(Maybe<P> m) => m.Map(p => p.Nick);");

        Assert.Empty(warnings);
    }

    [Fact]
    internal void Map_to_a_nullable_value_type_in_an_asynchronous_chain_gives_a_maybe_of_the_value_type()
    {
        var warnings = AnalyzerTestHost.NullabilityWarnings("Task<Maybe<int>> M(Task<Maybe<string>> m) => m.Map(v => (int?)v.Length);");

        Assert.Empty(warnings);
    }

    [Fact]
    internal void A_nullable_value_type_argument_is_reported_against_the_notnull_constraint()
    {
        var warnings = AnalyzerTestHost.NullabilityWarnings("Maybe<int?> M() => default;");

        Assert.Equal(["CS8714"], warnings);
    }

    [Fact]
    internal void From_accepts_a_nullable_value_and_a_nullable_reference()
    {
        var warnings = AnalyzerTestHost.NullabilityWarnings("void M(int? number, string? text) { Maybe<int> a = Maybe.From(number); Maybe<string> " +
            "b = Maybe.From(text); }");

        Assert.Empty(warnings);
    }

    [Fact]
    internal void TryGetValue_on_a_value_type_needs_no_null_check()
    {
        var warnings = AnalyzerTestHost.NullabilityWarnings("int M(Maybe<int> m) { if (m.TryGetValue(out var v)) { return v + 1; } return 0; }");

        Assert.Empty(warnings);
    }

    [Fact]
    internal void Zip_with_a_nullable_value_type_selector_gives_a_maybe_of_the_value_type()
    {
        var warnings = AnalyzerTestHost.NullabilityWarnings("Maybe<int> M(Maybe<int> a, Maybe<int> b) => a.Zip(b, (x, y) => (int?)(x + y));");

        Assert.Empty(warnings);
    }

    [Fact]
    internal void Zip_with_a_nullable_reference_selector_gives_a_maybe_of_the_reference_type()
    {
        var warnings = AnalyzerTestHost.NullabilityWarnings("Maybe<string> M(Maybe<int> a, Maybe<int> b) => a.Zip(b, (x, y) => (string?)null);");

        Assert.Empty(warnings);
    }

    [Fact]
    internal void Select_in_a_query_with_a_nullable_value_type_gives_a_maybe_of_the_value_type()
    {
        var warnings = AnalyzerTestHost.NullabilityWarnings("Maybe<int> M(Maybe<string> m) => from v in m select (int?)v.Length;");

        Assert.Empty(warnings);
    }
}
