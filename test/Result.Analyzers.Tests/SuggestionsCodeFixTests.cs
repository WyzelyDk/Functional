using Wyzely.Functional.ResultAnalyzers;

namespace Wyzely.Functional.ResultAnalyzersTests;

// A suggestion is only as good as its fix: the rewritten code must say the same thing, and the diagnostic must be gone afterwards.
public sealed class SuggestionsCodeFixTests
{
    private const string Sources = """
        static Result<int> Twice(int x) => Result.Success(x * 2);
        static bool Big(int x) => x > 5;
        static bool Ready() => true;
        static Error Failed() => Error.Create("Failed.");
        static readonly Error Rejected = Error.Create("Rejected.");
        """;

    private static readonly SuggestionsCodeFixProvider Provider = new();
    private static readonly ResultSuggestionsAnalyzer Analyzer = new();

    [Theory]
    [InlineData("WYZ0103", "Result<int> M(Result<int> r) => r.Bind(x => Ready() ? Twice(x) : Result.Success(x));", "r.BindIf(() => Ready(), x => Twice(x))")]
    [InlineData("WYZ0103", "Result<int> M(Result<int> r) => r.Bind(x => Big(x) ? Twice(x) : Result.Success(x));", "r.BindIf(x => Big(x), x => Twice(x))")]
    [InlineData("WYZ0103", "Result<int> M(Result<int> r) => r.Bind(x => Ready() ? Result.Success(x) : Twice(x));", "r.BindIf(() => !Ready(), x => Twice(x))")]
    [InlineData("WYZ0104", "Result<int> M(Result<int> r) => r.Map(x => Ready() ? x * 2 : x);", "r.MapIf(() => Ready(), x => x * 2)")]
    [InlineData("WYZ0104", "Result<int> M(Result<int> r) => r.Map(x => Ready() ? x : x * 2);", "r.MapIf(() => !Ready(), x => x * 2)")]
    [InlineData("WYZ0104", "Result<int> M(Result<int> r) => r.Map(x => x > 5 ? x + 1 : x);", "r.MapIf(x => x > 5, x => x + 1)")]
    [InlineData("WYZ0105", "Result<int> M(Result<int> r) => r.Ensure(x => !Big(x), Rejected);", "r.EnsureNot(x => Big(x), Rejected)")]
    [InlineData("WYZ0105", "Result<int> M(Result<int> r) => r.Ensure(x => !(x > 5), \"Too big.\");", "r.EnsureNot(x => x > 5, \"Too big.\")")]
    [InlineData(
        "WYZ0106",
        "Result<string> M(Result<string?> r) => r.Bind(x => x is null ? Result.Failure<string>(Rejected) : Result.Success(x));",
        "r.EnsureNotNull(Rejected)")]
    [InlineData(
        "WYZ0106",
        "Result<string> M(Result<string?> r) => r.Bind(x => x is not null ? Result.Success(x) : Result.Failure<string>(Failed()));",
        "r.EnsureNotNull(() => Failed())")]
    [InlineData(
        "WYZ0106",
        "Result<int> M(Result<int?> r) => r.Bind(x => x == null ? Result.Failure<int>(Rejected) : Result.Success(x.Value));",
        "r.EnsureNotNull(Rejected)")]
    [InlineData(
        "WYZ0107",
        "Result<int> M(Result<int> r) => r.Bind(a => Twice(a).Map(b => (a, b))).Map(t => t.Item1);",
        "r.BindZip(a => Twice(a)).Map(t => t.Item1)")]
    [InlineData(
        "WYZ0108",
        "Result<string> M(Result<int> r) { if (r.IsFailure) { return Result.Failure<string>(r.GetError()); } return Result.Success(\"ok\"); }",
        "return r.ConvertFailure<string>();")]
    [InlineData("WYZ0109", "Result M(bool ok) => ok ? Result.Success() : Result.Failure(Rejected);", "Result.SuccessIf(ok, Rejected)")]
    [InlineData("WYZ0109", "Result M(bool ok) => ok ? Result.Failure(Rejected) : Result.Success();", "Result.FailureIf(ok, Rejected)")]
    [InlineData("WYZ0109", "Result M(bool ok) => ok ? Result.Success() : Result.Failure(\"Nope.\");", "Result.SuccessIf(ok, \"Nope.\")")]
    [InlineData("WYZ0110", "Result<int> M(Result<string> r) => r.Bind(s => Result.Try(() => int.Parse(s)));", "r.MapTry(s => int.Parse(s))")]
    [InlineData(
        "WYZ0110",
        "Result<int> M(Result<string> r) => r.Bind(s => Result.Try(() => int.Parse(s), e => Rejected));",
        "r.MapTry(s => int.Parse(s), e => Rejected)")]
    [InlineData(
        "WYZ0110",
        "Task<Result<int>> M(Result<string> r) => r.Bind(s => Result.Try(async () => await Task.FromResult(s.Length)));",
        "r.MapTry(async s => await Task.FromResult(s.Length))")]
    internal async Task The_fix_rewrites_the_code_and_the_suggestion_is_gone(string id, string method, string expected)
    {
        var outcome = await CodeFixTestHost.FixAsync(Sources + "\n" + method, Provider, id, Analyzer);

        Assert.Multiple(
            () => Assert.NotNull(outcome.FixedSource),
            () => Assert.Contains(expected, outcome.FixedSource, StringComparison.Ordinal),
            () => Assert.DoesNotContain(id, outcome.RemainingIds));
    }

    [Fact]
    internal void The_provider_fixes_the_eight_suggestions_and_supports_fixing_all_occurrences()
    {
        Assert.Multiple(
            () => Assert.Equal(["WYZ0103", "WYZ0104", "WYZ0105", "WYZ0106", "WYZ0107", "WYZ0108", "WYZ0109", "WYZ0110"], Provider.FixableDiagnosticIds),
            () => Assert.NotNull(Provider.GetFixAllProvider()));
    }
}
