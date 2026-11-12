using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Wyzely.Functional.AnalyzerTesting;
using Wyzely.Functional.ResultAnalyzers;

namespace Wyzely.Functional.ResultAnalyzersTests;

// The rewritten code must say the same thing, and the suggestion must be gone afterwards.
public sealed class FallbackCodeFixTests
{
    private const string Sources = """
        static int Compute() => 42;
        static readonly Error Rejected = Error.Create("Rejected.");
        static Error Failed() => Error.Create("Failed.");
        sealed record Holder(Result<int> Value);
        Result<int> Field = Result.Success(2);
        """;

    private static readonly SuggestionsCodeFixProvider Provider = new();
    private static readonly ResultSuggestionsAnalyzer Analyzer = new();

    [Theory]
    [InlineData("Result<int> M(Result<int> r) => r.OrElse(e => Rejected);", "r.MapError(e => Rejected)")]
    [InlineData("Result<int> M(Result<int> r) => r.OrElse(() => Rejected);", "r.MapError(_ => Rejected)")]
    [InlineData("Result<int> M(Result<int> r) => r.OrElse(static () => Rejected);", "r.MapError(static _ => Rejected)")]
    [InlineData("Result<int> M(Result<int> r) => r.OrElse(() => { return Rejected; });", "r.MapError(_ => { return Rejected; })")]
    [InlineData("Result<int> M(Result<int> r) => r.OrElse(Result<int> () => Rejected);", "r.MapError(_ => Rejected)")]
    [InlineData("Result<int> M(Result<int> r) => r.OrElse(Result<int> (Error e) => Result.Failure<int>(Rejected));", "r.MapError((Error e) => Rejected)")]
    [InlineData("Result<int> M(Result<int> r) => r.OrElse(static e => Rejected);", "r.MapError(static e => Rejected)")]
    [InlineData("Result<int> M(Result<int> r) => r.OrElse((Error e) => Failed());", "r.MapError((Error e) => Failed())")]
    [InlineData("Result<int> M(Result<int> r) => r.OrElse(alternative: e => Rejected);", "r.MapError(e => Rejected)")]
    [InlineData("Result<int> M(Result<int> r) => r.OrElse(e => e.WithCode(\"A\"));", "r.MapError(e => e.WithCode(\"A\"))")]
    [InlineData("Result<int> M(Result<int> r) => r.OrElse(e => { return Rejected; });", "r.MapError(e => { return Rejected; })")]
    [InlineData("Result<int> M(Result<int> r) => r.OrElse(e => Result.Failure<int>(Rejected));", "r.MapError(e => Rejected)")]
    [InlineData("Result<int> M(Result<int> r) => r.OrElse(() => Result.Failure<int>(Failed()));", "r.MapError(_ => Failed())")]
    [InlineData("Result<int> M(Result<int> r) => r.OrElse(e => { return Result.Failure<int>(Rejected); });", "r.MapError(e => { return Rejected; })")]
    [InlineData("Result<int> M(Result<int> r) => r.OrElse(e => (Result.Failure<int>(Rejected)));", "r.MapError(e => Rejected)")]
    [InlineData("Result M(Result r) => r.OrElse(() => Result.Failure(Rejected));", "r.MapError(_ => Rejected)")]
    [InlineData("Result M(Result r) => r.OrElse(e => Rejected);", "r.MapError(e => Rejected)")]
    [InlineData("Task<Result<int>> M(Task<Result<int>> r) => r.OrElse(e => Rejected);", "r.MapError(e => Rejected)")]
    [InlineData("ValueTask<Result<int>> M(ValueTask<Result<int>> r) => r.OrElse(() => Failed());", "r.MapError(_ => Failed())")]
    [InlineData("Task<Result> M(Task<Result> r) => r.OrElse(e => Result.Failure(Rejected));", "r.MapError(e => Rejected)")]
    [InlineData("Result<Error> M(Result<Error> r) => r.OrElse(e => Result.Failure<Error>(Rejected));", "r.MapError(e => Rejected)")]
    internal async Task OrElse_that_only_returns_an_error_becomes_MapError(string method, string expected)
    {
        var outcome = await CodeFixTestHost.FixAsync(Sources + "\n" + method, Provider, "WYZ0118", Analyzer);

        Assert.Multiple(
            () => Assert.NotNull(outcome.FixedSource),
            () => Assert.Contains(expected, outcome.FixedSource, StringComparison.Ordinal),
            () => Assert.DoesNotContain("OrElse", outcome.FixedSource, StringComparison.Ordinal),
            () => Assert.DoesNotContain("WYZ0118", outcome.RemainingIds),
            () => Assert.Empty(CompilerErrors(outcome.FixedSource)));
    }

    [Theory]
    [InlineData("int M(Result<int> r) => r.IsSuccess ? r.GetValue() : 0;", "r.ValueOr(0)")]
    [InlineData("int M(Result<int> r) => (r.IsSuccess) ? r.GetValue() : 0;", "r.ValueOr(0)")]
    [InlineData("int M(Result<int> r) => r.IsSuccess ? r.GetValue() : (0);", "r.ValueOr(0)")]
    [InlineData("int M(Result<int> r, int fallback) => r.IsFailure ? fallback : r.GetValue();", "r.ValueOr(fallback)")]
    [InlineData("int M(Result<int> r) => r.IsSuccess ? r.GetValue() : default;", "r.ValueOr(default)")]
    [InlineData("int M() => Field.IsSuccess ? Field.GetValue() : 0;", "Field.ValueOr(0)")]
    [InlineData("int M() => this.Field.IsSuccess ? this.Field.GetValue() : 0;", "this.Field.ValueOr(0)")]
    [InlineData("int M(Holder h) => h.Value.IsSuccess ? h.Value.GetValue() : 0;", "h.Value.ValueOr(0)")]
    [InlineData("object M(Result<object> r, object fallback) => r.IsSuccess ? r.GetValue() : fallback;", "r.ValueOr(fallback)")]
    [InlineData("int M(Result<int> r) => r.IsSuccess ? r.GetValue() : Compute();", "r.ValueOr(() => Compute())")]
    [InlineData("int M(Result<int> r) => r.IsFailure ? Compute() : r.GetValue();", "r.ValueOr(() => Compute())")]
    [InlineData("List<int> M(Result<List<int>> r) => r.IsSuccess ? r.GetValue() : new List<int>();", "r.ValueOr(() => new List<int>())")]
    [InlineData("int M(Result<int> r, int add) => r.IsSuccess ? r.GetValue() : Compute() + add;", "r.ValueOr(() => Compute() + add)")]
    [InlineData(
        "int M(Result<int> r, Result<int> other) => r.IsSuccess ? r.GetValue() : other.GetError().Message.Length;",
        "r.ValueOr(() => other.GetError().Message.Length)")]
    [InlineData("int M(Result<int> r) => r.IsSuccess ? r.GetValue() : r.GetError().Message.Length;", "r.ValueOr(error => error.Message.Length)")]
    [InlineData(
        "string M(Result<string> r) => r.IsFailure ? \"Failed: \" + r.GetError().Message : r.GetValue();",
        "r.ValueOr(error => \"Failed: \" + error.Message)")]
    [InlineData(
        "int M(Result<int> r) => r.IsSuccess ? r.GetValue() : r.GetError().Message.Length + r.GetError().Message.Length;",
        "r.ValueOr(error => error.Message.Length + error.Message.Length)")]
    [InlineData(
        "int M(Result<int> r, Result<int> other) => r.IsSuccess ? r.GetValue() : r.GetError().Message.Length + other.GetError().Message.Length;",
        "r.ValueOr(error => error.Message.Length + other.GetError().Message.Length)")]
    [InlineData("int M() => Field.IsSuccess ? Field.GetValue() : Field.GetError().Message.Length;", "Field.ValueOr(error => error.Message.Length)")]
    [InlineData("Error M(Result<Error> r) => r.IsSuccess ? r.GetValue() : r.GetError();", "r.ValueOr(error => error)")]
    [InlineData(
        "string M(Result<string> r) => r.IsFailure ? r.GetError().Message.Trim() : r.GetValue();",
        "r.ValueOr(error => error.Message.Trim())")]
    [InlineData(
        "int M(Result<int> r) => r.IsSuccess ? r.GetValue() : r.GetError().Message.Length + Compute();",
        "r.ValueOr(error => error.Message.Length + Compute())")]
    internal async Task A_conditional_on_the_state_of_a_result_becomes_ValueOr(string method, string expected)
    {
        var outcome = await CodeFixTestHost.FixAsync(Sources + "\n" + method, Provider, "WYZ0119", Analyzer);

        Assert.Multiple(
            () => Assert.NotNull(outcome.FixedSource),
            () => Assert.Contains(expected, outcome.FixedSource, StringComparison.Ordinal),
            () => Assert.DoesNotContain("?", outcome.FixedSource, StringComparison.Ordinal),
            () => Assert.DoesNotContain("WYZ0119", outcome.RemainingIds),
            () => Assert.Empty(CompilerErrors(outcome.FixedSource)));
    }

    [Theory]
    [InlineData("int M(Result<int> r) => r.ValueOr(Compute());", "r.ValueOr(() => Compute())")]
    [InlineData("int M(Result<int> r) => r.ValueOr((Compute()));", "r.ValueOr(() => Compute())")]
    [InlineData("int M(Result<int> r) => r.ValueOr(fallback: Compute());", "r.ValueOr(fallback: () => Compute())")]
    [InlineData("int M(Result<int> r) => r.ValueOr(Compute() + 1);", "r.ValueOr(() => Compute() + 1)")]
    [InlineData("List<int> M(Result<List<int>> r) => r.ValueOr(new List<int>());", "r.ValueOr(() => new List<int>())")]
    [InlineData("List<int> M(Result<List<int>> r) => r.ValueOr([1, 2]);", "r.ValueOr(() => [1, 2])")]
    [InlineData("string M(Result<string> r, int id) => r.ValueOr($\"unknown {id}\");", "r.ValueOr(() => $\"unknown {id}\")")]
    [InlineData("int M(Holder h) => h.Value.ValueOr(Compute());", "h.Value.ValueOr(() => Compute())")]
    [InlineData("Task<int> M(Task<Result<int>> r) => r.ValueOr(Compute());", "r.ValueOr(() => Compute())")]
    [InlineData("ValueTask<int> M(ValueTask<Result<int>> r) => r.ValueOr(Compute());", "r.ValueOr(() => Compute())")]
    internal async Task ValueOr_with_a_fallback_that_does_work_gets_a_function(string method, string expected)
    {
        var outcome = await CodeFixTestHost.FixAsync(Sources + "\n" + method, Provider, "WYZ0120", Analyzer);

        Assert.Multiple(
            () => Assert.NotNull(outcome.FixedSource),
            () => Assert.Contains(expected, outcome.FixedSource, StringComparison.Ordinal),
            () => Assert.DoesNotContain("WYZ0120", outcome.RemainingIds),
            () => Assert.Empty(CompilerErrors(outcome.FixedSource)));
    }

    // A diagnostic can be made for code that has been edited since, and a rewrite that reads a property of the diagnostic checks the code again.
    [Theory]
    [InlineData("Result<int> M(Result<int> r) => r.OrElse(e => Rejected);", "r.OrElse(e => Rejected)")]
    [InlineData("Result<int> M(Result<int> r) => r.OrElse(e => Result.Failure<int>(a, b));", "r.OrElse(e => Result.Failure<int>(a, b))")]
    internal async Task No_fix_is_offered_for_a_wrapped_error_that_is_not_a_call_with_one_argument(string method, string marker)
    {
        var properties = ImmutableDictionary<string, string?>.Empty.Add("Wrapped", "true");

        var offered = await CodeFixTestHost.OfferedFixesAsync(Sources + "\n" + method, Provider, "WYZ0118", marker, properties);

        Assert.Equal(0, offered);
    }

    // The rewritten code has to compile, which also shows that the function binds to the overload that was meant.
    private static string[] CompilerErrors(string? source)
    {
        var compilation = CodeFixTesting.CreateCompilation(source!, AnalyzerTestHost.References());
        var errors = compilation.GetDiagnostics()
            .Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
            .Select(diagnostic => diagnostic.Id);

        return [.. errors];
    }
}
