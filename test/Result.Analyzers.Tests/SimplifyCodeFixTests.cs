using Wyzely.Functional.ResultAnalyzers;

namespace Wyzely.Functional.ResultAnalyzersTests;

// The simpler form must say the same thing, and the suggestion must be gone afterwards.
public sealed class SimplifyCodeFixTests
{
    private const string Sources = """
        static Result<int> Twice(int x) => Result.Success(x * 2);
        static int Compute() => 42;
        static readonly Error Rejected = Error.Create("Rejected.");
        """;

    private static readonly SuggestionsCodeFixProvider Provider = new();
    private static readonly ResultSuggestionsAnalyzer Analyzer = new();

    [Theory]
    [InlineData("WYZ0112", "Result<int> M(Result<int> r) => r.Bind(x => Result.Success(x * 2));", "r.Map(x => x * 2)")]
    [InlineData("WYZ0112", "Result<int> M(Result<int> r) => r.Bind(x => { return Result.Success(x + 1); });", "r.Map(x => { return x + 1; })")]
    [InlineData("WYZ0112", "Task<Result<int>> M(Task<Result<int>> r) => r.Bind(x => Result.Success(x * 2));", "r.Map(x => x * 2)")]
    [InlineData(
        "WYZ0113",
        "Result<List<int>> M(Result<List<int>> r) => r.Map(l => { l.Add(1); return l; });",
        "r.Tap(l => { l.Add(1); })")]
    [InlineData("WYZ0114", "int M(Result<int> r) => r.Match(v => v, _ => 0);", "r.ValueOr(0)")]
    [InlineData("WYZ0114", "string M(Result<string> r, string fallback) => r.Match(v => v, e => fallback);", "r.ValueOr(fallback)")]
    [InlineData("WYZ0114", "int M(Result<int> r) => r.Match(v => v, _ => Compute());", "r.ValueOr(_ => Compute())")]
    [InlineData("WYZ0114", "int M(Result<int> r) => r.Match(v => v, e => e.Message.Length);", "r.ValueOr(e => e.Message.Length)")]
    [InlineData("WYZ0114", "Task<int> M(Task<Result<int>> r) => r.Match(v => v, _ => 0);", "r.ValueOr(0)")]
    [InlineData("WYZ0114", "Task<int> M(Task<Result<int>> r) => r.Match(v => v, e => e.Message.Length);", "r.ValueOr(e => e.Message.Length)")]
    [InlineData("WYZ0114", "ValueTask<int> M(ValueTask<Result<int>> r) => r.Match(v => v, _ => 0);", "r.ValueOr(0)")]
    [InlineData("WYZ0114", "Error M(Result<Error> r) => r.Match(v => v, e => e);", "r.ValueOr(e => e)")]
    [InlineData("WYZ0115", "Result<int> M(Result<int> r) => r.Map(x => x);", "=> r;")]
    [InlineData("WYZ0115", "Result<int> M(Result<int> r) => r.Bind(x => Result.Success(x));", "=> r;")]
    [InlineData("WYZ0115", "Result<int> M(Result<int> r) => r.Bind(Twice).Map(x => x).Map(x => x + 1);", "r.Bind(Twice).Map(x => x + 1)")]
    [InlineData("WYZ0115", "Result<int> M(Result<int> r) => r.MapError(e => e);", "=> r;")]
    [InlineData("WYZ0115", "Result M(Result r) => r.MapError(e => e);", "=> r;")]
    [InlineData("WYZ0115", "Task<Result<int>> M(Task<Result<int>> r) => r.MapError(e => e);", "=> r;")]
    [InlineData("WYZ0115", "ValueTask<Result<int>> M(ValueTask<Result<int>> r) => r.MapError(e => e);", "=> r;")]
    [InlineData("WYZ0115", "Result<int> M(Result<int> r) => r.OrElse(e => e);", "=> r;")]
    [InlineData("WYZ0115", "Result<int> M(Result<int> r) => r.OrElse(e => Result.Failure<int>(e));", "=> r;")]
    [InlineData("WYZ0115", "Result M(Result r) => r.OrElse(e => Result.Failure(e));", "=> r;")]
    [InlineData("WYZ0115", "Task<Result<int>> M(Task<Result<int>> r) => r.OrElse(e => e);", "=> r;")]
    [InlineData(
        "WYZ0115",
        "Result<int> M(Result<int> r) => r.Bind(Twice).MapError(e => e).Map(x => x + 1);",
        "r.Bind(Twice).Map(x => x + 1)")]
    [InlineData("WYZ0116", "Result M() => Result.Failure(Error.Create(\"Nope.\"));", "Result.Failure(\"Nope.\")")]
    [InlineData("WYZ0116", "Result<int> M(string text) => Result.Failure<int>(Error.Create(text));", "Result.Failure<int>(text)")]
    [InlineData("WYZ0117", "Result<int> M(Result<Result<int>> r) => r.Bind(x => x);", "r.Flatten()")]
    [InlineData("WYZ0117", "Task<Result<int>> M(Task<Result<Result<int>>> r) => r.Bind(x => x);", "r.Flatten()")]
    internal async Task The_fix_rewrites_the_code_and_the_suggestion_is_gone(string id, string method, string expected)
    {
        var outcome = await CodeFixTestHost.FixAsync(Sources + "\n" + method, Provider, id, Analyzer);

        Assert.Multiple(
            () => Assert.NotNull(outcome.FixedSource),
            () => Assert.Contains(expected, outcome.FixedSource, StringComparison.Ordinal),
            () => Assert.DoesNotContain(id, outcome.RemainingIds));
    }
}
