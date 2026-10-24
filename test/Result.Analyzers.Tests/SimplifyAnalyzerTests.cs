using Wyzely.Functional.ResultAnalyzers;

namespace Wyzely.Functional.ResultAnalyzersTests;

// The simplify rules point at code that does more than it has to. A rewrite must give exactly the same result and type, so a shape that looks
// similar but is not the same must stay silent.
public sealed class SimplifyAnalyzerTests
{
    private const string Sources = """
        static Result<int> Twice(int x) => Result.Success(x * 2);
        static int Compute() => 42;
        """;

    private static readonly ResultSuggestionsAnalyzer Analyzer = new();

    [Theory]
    [InlineData("WYZ0112", "Result<int> M(Result<int> r) => r.Bind(x => Result.Success(x * 2));")]
    [InlineData("WYZ0112", "Result<int> M(Result<int> r) => r.Bind(x => { return Result.Success(x + 1); });")]
    [InlineData("WYZ0112", "Task<Result<int>> M(Task<Result<int>> r) => r.Bind(x => Result.Success(x * 2));")]
    [InlineData("WYZ0113", "Result<List<int>> M(Result<List<int>> r) => r.Map(l => { l.Add(1); return l; });")]
    [InlineData("WYZ0113", "Task<Result<List<int>>> M(Task<Result<List<int>>> r) => r.Map(l => { l.Add(1); l.Add(2); return l; });")]
    [InlineData("WYZ0114", "int M(Result<int> r) => r.Match(v => v, _ => 0);")]
    [InlineData("WYZ0114", "string M(Result<string> r, string fallback) => r.Match(v => v, e => fallback);")]
    [InlineData("WYZ0115", "Result<int> M(Result<int> r) => r.Map(x => x);")]
    [InlineData("WYZ0115", "Result<int> M(Result<int> r) => r.Map(x => { return x; });")]
    [InlineData("WYZ0115", "Task<Result<int>> M(Task<Result<int>> r) => r.Map(x => x);")]
    [InlineData("WYZ0115", "Result<int> M(Result<int> r) => r.Bind(x => Result.Success(x));")]
    [InlineData("WYZ0116", "Result M() => Result.Failure(Error.Create(\"Nope.\"));")]
    [InlineData("WYZ0116", "Result<int> M(string text) => Result.Failure<int>(Error.Create(text));")]
    [InlineData("WYZ0117", "Result<int> M(Result<Result<int>> r) => r.Bind(x => x);")]
    [InlineData("WYZ0117", "Result<int> M(Result<Result<int>> r) => r.Bind(static x => x);")]
    [InlineData("WYZ0117", "Task<Result<int>> M(Task<Result<Result<int>>> r) => r.Bind(x => x);")]
    internal async Task A_shape_with_a_simpler_form_is_reported(string id, string method)
    {
        var ids = await AnalyzerTestHost.AnalyzeAsync(Sources + "\n" + method, Analyzer);

        Assert.Equal([id], ids);
    }

    // Each of these looks like one of the shapes above and would change the result, the type or the order of evaluation when rewritten.
    [Theory]
    [InlineData("Result<int> M(Result<int> r) => r.Bind(x => Twice(x));")]
    [InlineData("Result<object> M(Result<int> r) => r.Bind(x => Result.Success<object>(x * 2));")]
    [InlineData("Task<Result<int>> M(Result<int> r) => r.Bind(async x => Result.Success(await Task.FromResult(x)));")]
    [InlineData("Result<List<int>> M(Result<List<int>> r) => r.Map(l => { l.Add(1); return new List<int>(l); });")]
    [InlineData("Result<int> M(Result<int> r) => r.Map(n => { n++; return n; });")]
    [InlineData("Result<List<int>> M(Result<List<int>> r) => r.Map(l => { if (l.Count > 0) { return l; } l.Add(1); return l; });")]
    [InlineData("Task<Result<List<int>>> M(Result<List<int>> r) => r.Map(async l => { await Task.Yield(); l.Add(1); return l; });")]
    [InlineData("int M(Result<int> r) => r.Match(v => v, _ => Compute());")]
    [InlineData("int M(Result<int> r) => r.Match(v => v, e => e.Message.Length);")]
    [InlineData("int M(Result<int> r) => r.Match(v => v + 1, _ => 0);")]
    [InlineData("object M(Result<int> r) => r.Match<int, object>(v => v, _ => 0);")]
    [InlineData("Task<int> M(Task<Result<int>> r) => r.Match(v => v, _ => 0);")]
    [InlineData("Result<int> M(Result<int> r) => r.Map(x => x + 0);")]
    [InlineData("Result<object> M(Result<int> r) => r.Map<int, object>(x => x);")]
    [InlineData("Result<object> M(Result<int> r) => r.Bind(x => Result.Success<object>(x));")]
    [InlineData("Result M() => Result.Failure(Error.Create(\"Nope.\", ErrorKind.NotFound));")]
    [InlineData("Result M() => Result.Failure(Error.NotFound(\"Nope.\"));")]
    [InlineData("Result M() => Result.Failure(Error.Create(null!));")]
    [InlineData("Result<Result<int>> M(Result<Result<int>> r) => r.Bind(x => Twice(1).Map(y => x));")]
    internal async Task Shapes_that_would_change_the_result_or_do_not_match_are_left_alone(string method)
    {
        var ids = await AnalyzerTestHost.AnalyzeAsync(Sources + "\n" + method, Analyzer);

        Assert.Empty(ids);
    }
}
