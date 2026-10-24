using Wyzely.Functional.ResultAnalyzers;

namespace Wyzely.Functional.ResultAnalyzersTests;

// The one fix for WYZ0102 is a guard clause: when GetValue() is read in a method that returns a result, the failure is returned first. It is
// offered only where adding the early return cannot change what else the statement does.
public sealed class UncheckedAccessCodeFixTests
{
    private const string Sources = """
        static Result<int> Compute() => Result.Success(1);
        Result<int> Field = Result.Success(2);
        """;

    private static readonly UncheckedAccessCodeFixProvider Provider = new();

    [Theory]
    [InlineData(
        "Result<int> M(Result<int> r) { var v = r.GetValue(); return Result.Success(v * 2); }",
        "if (r.IsFailure)",
        "return r;")]
    [InlineData(
        "Result<string> M(Result<int> r) { var v = r.GetValue(); return Result.Success(v.ToString()); }",
        "if (r.IsFailure)",
        "return r.ConvertFailure<string>();")]
    [InlineData(
        "Result M(Result<int> r) { var v = r.GetValue(); Console.WriteLine(v); return Result.Success(); }",
        "if (r.IsFailure)",
        "return Result.Failure(r.GetError());")]
    [InlineData(
        "async Task<Result<string>> M(Result<int> r) { var v = r.GetValue(); await Task.Yield(); return Result.Success(v.ToString()); }",
        "if (r.IsFailure)",
        "return r.ConvertFailure<string>();")]
    [InlineData(
        "Result<int> M(Result<string> r) { var length = r.GetValue().Length; return Result.Success(length); }",
        "if (r.IsFailure)",
        "return r.ConvertFailure<int>();")]
    [InlineData(
        "Result<int> M() { var v = Field.GetValue(); return Result.Success(v); }",
        "if (Field.IsFailure)",
        "return Field;")]
    [InlineData(
        "Result<int> M(Result<int> r, bool b) { if (b) { var v = r.GetValue(); return Result.Success(v); } return r; }",
        "if (r.IsFailure)",
        "return r;")]
    [InlineData(
        "Result<int> M(Result<int> r) { int v; v = r.GetValue(); return Result.Success(v); }",
        "if (r.IsFailure)",
        "return r;")]
    internal async Task The_fix_returns_the_failure_before_the_statement_and_the_warning_is_gone(string method, string guard, string returned)
    {
        var outcome = await CodeFixTestHost.FixAsync(Sources + "\n" + method, Provider, "WYZ0102");

        Assert.Multiple(
            () => Assert.NotNull(outcome.FixedSource),
            () => Assert.Contains(guard, outcome.FixedSource, StringComparison.Ordinal),
            () => Assert.Contains(returned, outcome.FixedSource, StringComparison.Ordinal),
            () => Assert.True(
                outcome.FixedSource!.IndexOf(guard, StringComparison.Ordinal) < outcome.FixedSource.IndexOf("GetValue()", StringComparison.Ordinal),
                "The guard has to come before the call."),
            () => Assert.DoesNotContain("WYZ0102", outcome.RemainingIds));
    }

    [Fact]
    internal async Task The_guard_is_a_block_on_its_own_lines_above_the_statement_and_the_comment_stays_with_the_statement()
    {
        const string method = "    Result<string> M(Result<int> r)\n    {\n        // The value.\n        var v = r.GetValue();\n"
            + "        return Result.Success(v.ToString());\n    }";

        var outcome = await CodeFixTestHost.FixAsync(Sources + "\n" + method, Provider, "WYZ0102");

        Assert.Contains(
            "    {\n        if (r.IsFailure)\n        {\n            return r.ConvertFailure<string>();\n        }\n        // The value.\n"
            + "        var v = r.GetValue();",
            outcome.FixedSource?.ReplaceLineEndings("\n"),
            StringComparison.Ordinal);
    }

    // Each of these would change what runs, or has no single obvious thing to return, so a guard clause is not offered.
    [Theory]
    [InlineData("int M(Result<int> r) => r.GetValue();")]
    [InlineData("int M(Result<int> r) { var v = r.GetValue(); return v; }")]
    [InlineData("Error M(Result<int> r) { var e = r.GetError(); return e; }")]
    [InlineData("Result<string> M(Result<int> r) { var e = r.ConvertFailure<string>(); return e; }")]
    [InlineData("Result<int> M(Result<int> r) { Func<int> f = () => { var v = r.GetValue(); return v; }; return Result.Success(f()); }")]
    [InlineData("Result<int> M(Result<int> r, bool b) { var v = b ? r.GetValue() : 0; return Result.Success(v); }")]
    [InlineData("Result<int> M(Result<int> r, bool b) { var v = b && r.GetValue() > 0; return Result.Success(v ? 1 : 0); }")]
    [InlineData("Result<int> M(Result<int> r) { var v = Compute().GetValue() + 1; return Result.Success(v); }")]
    [InlineData("Result<int> M(Result<int> r) { var v = Compute().GetValue(); return Result.Success(v); }")]
    [InlineData("Result<int> M(Result<int> r) { var v = 1 + r.GetValue(); return Result.Success(v); }")]
    [InlineData("Result<int> M(Result<int> r) { return Result.Success(Math.Abs(r.GetValue())); }")]
    [InlineData("Task<Result<int>> M(Result<int> r) { var v = r.GetValue(); return Task.FromResult(Result.Success(v)); }")]
    internal async Task No_fix_is_offered_where_a_guard_clause_could_change_the_behavior(string method)
    {
        var outcome = await CodeFixTestHost.FixAsync(Sources + "\n" + method, Provider, "WYZ0102");

        Assert.Null(outcome.FixedSource);
    }

    [Fact]
    internal void The_provider_fixes_the_rule_and_does_not_offer_fixing_all_occurrences()
    {
        Assert.Multiple(
            () => Assert.Equal(["WYZ0102"], Provider.FixableDiagnosticIds),
            () => Assert.Null(Provider.GetFixAllProvider()));
    }
}
