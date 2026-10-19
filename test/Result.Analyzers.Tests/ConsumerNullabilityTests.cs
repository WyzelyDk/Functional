namespace Wyzely.Functional.ResultAnalyzersTests;

// The nullable annotations are part of the API: these tests compile consumer code with nullable reference types enabled and
// check which warnings the compiler reports, in both directions (what is accepted and what is rejected).
public sealed class ConsumerNullabilityTests
{
    [Fact]
    internal void TryGetValue_makes_the_value_non_null_when_it_returns_true()
    {
        var warnings = AnalyzerTestHost.NullabilityWarnings("int M(Result<string> r) { if (r.TryGetValue(out var v)) { return v.Length; } return 0; }");

        Assert.Empty(warnings);
    }

    [Fact]
    internal void TryGetValue_leaves_the_value_maybe_null_when_it_returns_false()
    {
        var warnings = AnalyzerTestHost.NullabilityWarnings("int M(Result<string> r) { if (!r.TryGetValue(out var v)) { return v.Length; } return 0; }");

        Assert.Equal(["CS8602"], warnings);
    }

    [Fact]
    internal void TryGetError_makes_the_error_non_null_when_it_returns_true()
    {
        var warnings = AnalyzerTestHost.NullabilityWarnings("string M(Result r) { if (r.TryGetError(out var e)) { return e.Message; } return \"\"; }");

        Assert.Empty(warnings);
    }

    [Fact]
    internal void TryGetError_of_a_typed_result_makes_the_error_non_null_when_it_returns_true()
    {
        var warnings = AnalyzerTestHost.NullabilityWarnings("string M(Result<int> r) { if (r.TryGetError(out var e)) { return e.Message; } return \"\"; }");

        Assert.Empty(warnings);
    }

    [Fact]
    internal void TryGetError_leaves_the_error_maybe_null_when_it_returns_false()
    {
        var warnings = AnalyzerTestHost.NullabilityWarnings("string M(Result r) { if (!r.TryGetError(out var e)) { return e.Message; } return \"\"; }");

        Assert.Equal(["CS8602"], warnings);
    }

    [Fact]
    internal void A_nullable_value_type_argument_keeps_its_nullability()
    {
        var warnings = AnalyzerTestHost.NullabilityWarnings("int M(Result<string?> r) { if (r.TryGetValue(out var v)) { return v.Length; } return 0; }");

        Assert.Equal(["CS8602"], warnings);
    }

    [Fact]
    internal void A_success_may_hold_null_when_the_value_type_is_nullable()
    {
        var warnings = AnalyzerTestHost.NullabilityWarnings("Result<string?> M() => Result.Success<string?>(null);");

        Assert.Empty(warnings);
    }

    [Fact]
    internal void A_null_value_is_rejected_for_a_non_nullable_value_type()
    {
        var warnings = AnalyzerTestHost.NullabilityWarnings("Result<string> M() => Result.Success<string>(null);");

        Assert.Equal(["CS8625"], warnings);
    }

    [Fact]
    internal void A_null_error_is_rejected()
    {
        var warnings = AnalyzerTestHost.NullabilityWarnings("Result M(Error? error) => Result.Failure(error);");

        Assert.Equal(["CS8604"], warnings);
    }

    [Fact]
    internal void Deconstruction_leaves_the_value_and_the_error_maybe_null()
    {
        var warnings = AnalyzerTestHost.NullabilityWarnings("int M(Result<string> r) { var (v, e) = r; return v.Length + e.Message.Length; }");

        Assert.Equal(["CS8602", "CS8602"], warnings);
    }

    [Fact]
    internal void ValueOr_with_a_non_null_fallback_is_non_null()
    {
        var warnings = AnalyzerTestHost.NullabilityWarnings("int M(Result<string> r) => r.ValueOr(\"none\").Length;");

        Assert.Empty(warnings);
    }

    [Fact]
    internal void The_value_passed_to_a_delegate_is_non_null_for_a_non_nullable_value_type()
    {
        var warnings = AnalyzerTestHost.NullabilityWarnings("Result<int> M(Result<string> r) => r.Map(v => v.Length);");

        Assert.Empty(warnings);
    }

    [Fact]
    internal void The_value_passed_to_a_delegate_is_maybe_null_for_a_nullable_value_type()
    {
        var warnings = AnalyzerTestHost.NullabilityWarnings("Result<int> M(Result<string?> r) => r.Map(v => v.Length);");

        Assert.Equal(["CS8602"], warnings);
    }

    [Fact]
    internal void The_error_passed_to_a_delegate_is_non_null()
    {
        var warnings = AnalyzerTestHost.NullabilityWarnings("string M(Result<int> r) => r.Match(v => \"ok\", e => e.Message);");

        Assert.Empty(warnings);
    }

    [Fact]
    internal void The_optional_parts_of_an_error_are_maybe_null_and_the_message_is_not()
    {
        var warnings = AnalyzerTestHost.NullabilityWarnings(
            "int M(Error e) => e.Message.Length + e.Template.Length + e.Code.Length + e.Exception.Message.Length;");

        Assert.Equal(["CS8602", "CS8602"], warnings);
    }

    [Fact]
    internal void An_async_receiver_keeps_the_nullability_of_its_value()
    {
        var warnings = AnalyzerTestHost.NullabilityWarnings(
            "Task<Result<int>> M(Task<Result<string>> r) => r.Map(v => v.Length);");

        Assert.Empty(warnings);
    }
}
