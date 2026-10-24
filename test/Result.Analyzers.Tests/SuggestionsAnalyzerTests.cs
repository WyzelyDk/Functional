using Wyzely.Functional.ResultAnalyzers;

namespace Wyzely.Functional.ResultAnalyzersTests;

// The suggestion rules recognise exact shapes only: a snippet that looks similar but would change the behavior of the code must stay silent.
public sealed class SuggestionsAnalyzerTests
{
    private const string Sources = """
        static Result<int> Twice(int x) => Result.Success(x * 2);
        static bool Big(int x) => x > 5;
        static bool Ready() => true;
        static Error Failed() => Error.Create("Failed.");
        static readonly Error Rejected = Error.Create("Rejected.");
        """;

    private static readonly ResultSuggestionsAnalyzer Analyzer = new();

    [Theory]
    [InlineData("Result<int> M(Result<int> r) => r.Bind(x => Ready() ? Twice(x) : Result.Success(x));")]
    [InlineData("Result<int> M(Result<int> r) => r.Bind(x => Ready() ? Result.Success(x) : Twice(x));")]
    [InlineData("Result<int> M(Result<int> r) => r.Bind(x => Big(x) ? Twice(x) : Result.Success(x));")]
    [InlineData("Task<Result<int>> M(Task<Result<int>> r) => r.Bind(x => Ready() ? Twice(x) : Result.Success(x));")]
    [InlineData("Result<string> M(Result<string?> r) => r.Bind(x => x is null ? Result.Success(\"none\") : Result.Success(x));")]
    internal async Task A_condition_inside_Bind_that_returns_the_value_unchanged_is_a_BindIf(string method)
    {
        var ids = await AnalyzerTestHost.AnalyzeAsync(Sources + "\n" + method, Analyzer);

        Assert.Equal(["WYZ0103"], ids);
    }

    [Theory]
    [InlineData("Result<int> M(Result<int> r) => r.Map(x => Ready() ? x * 2 : x);")]
    [InlineData("Result<int> M(Result<int> r) => r.Map(x => Ready() ? x : x * 2);")]
    [InlineData("Result<int> M(Result<int> r) => r.Map(x => Big(x) ? x + 1 : x);")]
    internal async Task A_condition_inside_Map_that_returns_the_value_unchanged_is_a_MapIf(string method)
    {
        var ids = await AnalyzerTestHost.AnalyzeAsync(Sources + "\n" + method, Analyzer);

        Assert.Equal(["WYZ0104"], ids);
    }

    [Theory]
    [InlineData("Result<int> M(Result<int> r) => r.Ensure(x => !Big(x), Rejected);")]
    [InlineData("Result<int> M(Result<int> r) => r.Ensure(x => !(x > 5), \"Too big.\");")]
    [InlineData("Task<Result<int>> M(Task<Result<int>> r) => r.Ensure(x => !Big(x), Rejected);")]
    internal async Task Ensure_with_a_negated_condition_is_EnsureNot(string method)
    {
        var ids = await AnalyzerTestHost.AnalyzeAsync(Sources + "\n" + method, Analyzer);

        Assert.Equal(["WYZ0105"], ids);
    }

    [Theory]
    [InlineData("Result<string> M(Result<string?> r) => r.Bind(x => x is null ? Result.Failure<string>(Rejected) : Result.Success(x));")]
    [InlineData("Result<string> M(Result<string?> r) => r.Bind(x => x == null ? Result.Failure<string>(Rejected) : Result.Success(x));")]
    [InlineData("Result<string> M(Result<string?> r) => r.Bind(x => x is not null ? Result.Success(x) : Result.Failure<string>(Rejected));")]
    [InlineData("Result<int> M(Result<int?> r) => r.Bind(x => x is null ? Result.Failure<int>(Rejected) : Result.Success(x.Value));")]
    internal async Task A_null_check_inside_Bind_is_EnsureNotNull(string method)
    {
        var ids = await AnalyzerTestHost.AnalyzeAsync(Sources + "\n" + method, Analyzer);

        Assert.Equal(["WYZ0106"], ids);
    }

    [Theory]
    [InlineData("Result<int> M(Result<int> r) => r.Bind(a => Twice(a).Map(b => (a, b))).Map(t => t.Item1);")]
    [InlineData("Task<Result<int>> M(Task<Result<int>> r) => r.Bind(a => Twice(a).Map(b => (a, b))).Map(t => t.Item2);")]
    internal async Task Bind_then_Map_into_a_tuple_is_a_BindZip_when_the_names_of_the_tuple_are_not_used(string method)
    {
        var ids = await AnalyzerTestHost.AnalyzeAsync(Sources + "\n" + method, Analyzer);

        Assert.Equal(["WYZ0107"], ids);
    }

    [Theory]
    [InlineData("Result<string> M(Result<int> r) { if (r.IsFailure) { return Result.Failure<string>(r.GetError()); } return Result.Success(\"ok\"); }")]
    [InlineData("Result<string> M(Result r) { if (r.IsFailure) { return Result.Failure<string>(r.GetError()); } return Result.Success(\"ok\"); }")]
    internal async Task Failure_of_the_error_of_a_result_is_ConvertFailure(string method)
    {
        var ids = await AnalyzerTestHost.AnalyzeAsync(Sources + "\n" + method, Analyzer);

        Assert.Equal(["WYZ0108"], ids);
    }

    [Theory]
    [InlineData("Result M(bool ok) => ok ? Result.Success() : Result.Failure(Rejected);")]
    [InlineData("Result M(bool ok) => ok ? Result.Failure(Rejected) : Result.Success();")]
    [InlineData("Result M(bool ok) => ok ? Result.Success() : Result.Failure(\"Nope.\");")]
    internal async Task A_conditional_between_a_success_and_a_failure_is_SuccessIf_or_FailureIf(string method)
    {
        var ids = await AnalyzerTestHost.AnalyzeAsync(Sources + "\n" + method, Analyzer);

        Assert.Equal(["WYZ0109"], ids);
    }

    [Theory]
    [InlineData("Result<int> M(Result<string> r) => r.Bind(s => Result.Try(() => int.Parse(s)));")]
    [InlineData("Result<int> M(Result<string> r) => r.Bind(s => Result.Try(() => int.Parse(s), e => Rejected));")]
    [InlineData("Task<Result<int>> M(Result<string> r) => r.Bind(s => Result.Try(async () => await Task.FromResult(s.Length)));")]
    internal async Task Bind_with_Result_Try_inside_is_MapTry(string method)
    {
        var ids = await AnalyzerTestHost.AnalyzeAsync(Sources + "\n" + method, Analyzer);

        Assert.Equal(["WYZ0110"], ids);
    }

    // Each of these looks like one of the shapes above and would change the behavior or the type when rewritten, or is just something else.
    [Theory]
    [InlineData("Result<int> M(Result<int> r) => r.Bind(x => Ready() ? Twice(x) : Result.Failure<int>(Rejected));")]
    [InlineData("Result<int> M(Result<int> r) => r.Bind(x => Ready() ? Twice(x) : Result.Success(x + 1));")]
    [InlineData("Result<int> M(Result<int> r) => r.Bind(x => Twice(x));")]
    [InlineData("Result<int> M(Result<int> r) => r.Bind(x => { var y = x + 1; return Ready() ? Twice(y) : Result.Success(y); });")]
    [InlineData("Result<int> M(Result<int> r) => r.Map(x => Ready() ? x * 2 : x + 1);")]
    [InlineData("Result<double> M(Result<int> r) => r.Map(x => Ready() ? 1.5 : x);")]
    [InlineData("Result<int> M(Result<int> r) => r.Map(x => x * 2);")]
    [InlineData("Result<int> M(Result<int> r) => r.Ensure(x => Big(x), Rejected);")]
    [InlineData("Result<int> M(Result<int> r) => r.Ensure(x => !Big(x), x => Failed());")]
    [InlineData("Result<int> M(Result<int> r) => r.Ensure(x => !Big(x) && Ready(), Rejected);")]
    [InlineData("Result<string> M(Result<string?> r) => r.Bind(x => x is null ? Result.Failure<string>(Rejected) : Result.Success(x.Trim()));")]
    [InlineData("Result<string> M(Result<string?> r) => r.Bind(x => x?.Length > 3 ? Result.Success(x) : Result.Failure<string>(Rejected));")]
    [InlineData("Result<int> M(Result<int> r) => r.Bind(a => Twice(a).Map(b => (a, b))).Map(t => t.a + t.b);")]
    [InlineData("Result<(int, int)> M(Result<int> r) => r.Bind(a => Twice(a).Map(b => (a, b)));")]
    [InlineData("Result<int> M(Result<int> r) => r.Bind(a => Twice(a).Map(b => (b, a))).Map(t => t.Item1);")]
    [InlineData("Result<int> M(Result<int> r) => r.Bind(a => Twice(a).Map(b => (a, b, 1))).Map(t => t.Item1);")]
    [InlineData("Result<string> M(Result<int> r) => Result.Failure<string>(Rejected);")]
    [InlineData("Result M(Result<int> r) { if (r.IsFailure) { return Result.Failure(r.GetError()); } return Result.Success(); }")]
    [InlineData("Result M() => Ready() ? Result.Success() : Result.Failure(Failed());")]
    [InlineData("Result M(IReadOnlyList<Error> errors) => errors.Count == 0 ? Result.Success() : Result.Failure(Error.Aggregate(errors));")]
    [InlineData("Result<int> M(bool ok) => ok ? Result.Success(1) : Result.Failure<int>(Rejected);")]
    [InlineData("Result M(bool ok) => ok ? Result.Success() : Result.Success();")]
    [InlineData("Result M(bool ok) => ok ? Result.Failure(Rejected) : Result.Failure(Failed());")]
    [InlineData("Result M(Result<string> r) => r.Bind(s => Result.Try(() => Console.WriteLine(s)));")]
    [InlineData("Result<int> M(Result<string> r) => r.Bind(s => Result.Try(() => int.Parse(s)).Map(n => n + 1));")]
    internal async Task Shapes_that_would_change_the_behavior_or_do_not_match_are_left_alone(string method)
    {
        var ids = await AnalyzerTestHost.AnalyzeAsync(Sources + "\n" + method, Analyzer);

        Assert.Empty(ids);
    }

    [Fact]
    internal void Every_rule_is_an_info_suggestion_in_the_design_category_with_a_help_link_to_its_page()
    {
        Assert.Multiple(
            () => Assert.Equal(8, Analyzer.SupportedDiagnostics.Length),
            () => Assert.All(
                Analyzer.SupportedDiagnostics,
                descriptor => Assert.Equal(Microsoft.CodeAnalysis.DiagnosticSeverity.Info, descriptor.DefaultSeverity)),
            () => Assert.All(Analyzer.SupportedDiagnostics, descriptor => Assert.Equal("Design", descriptor.Category)),
            () => Assert.All(
                Analyzer.SupportedDiagnostics,
                descriptor => Assert.EndsWith("/docs/result/analyzers/" + descriptor.Id + ".md", descriptor.HelpLinkUri, StringComparison.Ordinal)));
    }
}
