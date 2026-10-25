using Wyzely.Functional.ResultAnalyzers;

namespace Wyzely.Functional.ResultAnalyzersTests;

// The edges of the rules: the guards of an access that are written in other ways, code that cannot be analyzed, shapes that look like a suggestion
// and are not, and a compilation that does not know the library.
public sealed class AnalyzerEdgeCaseTests
{
    private const string Sources = """
        static readonly Error Rejected = Error.Create("Rejected.");
        static void Bump(ref int x) => x++;
        static Result Fail(Error error) => Result.Failure(error);
        """;

    private static readonly ResultSuggestionsAnalyzer Suggestions = new();

    [Fact]
    internal async Task A_conditional_access_cannot_be_analyzed_and_is_not_reported()
    {
        var ids = await AnalyzerTestHost.AnalyzeAsync("int? M(Result<int>? r) => r?.GetValue();");

        Assert.Empty(ids);
    }

    [Fact]
    internal async Task An_access_in_a_field_initializer_has_nothing_around_it_to_guard_it_and_is_reported()
    {
        var ids = await AnalyzerTestHost.AnalyzeAsync("static Result<int> Source; static int Value = Source.GetValue();");

        Assert.Equal(["WYZ0102"], ids);
    }

    [Fact]
    internal async Task An_early_exit_with_an_empty_block_does_not_guard_the_access()
    {
        var ids = await AnalyzerTestHost.AnalyzeAsync("int M(Result<int> r) { if (r.IsFailure) { } return r.GetValue(); }");

        Assert.Equal(["WYZ0102"], ids);
    }

    [Theory]
    [InlineData("int M(Result<int> r) { if ((r.IsFailure)) { return 0; } return r.GetValue(); }")]
    [InlineData("int M(Result<int> r, bool other) { if (other || r.IsFailure) { return 0; } return r.GetValue(); }")]
    [InlineData("int M(Result<int> r, bool other) { if (r.IsFailure || other) { return 0; } return r.GetValue(); }")]
    [InlineData("int M(Result<int> r, bool other) { if (r.IsSuccess && other) { return r.GetValue(); } return 0; }")]
    [InlineData("int M(Result<int> r, bool other) { if (other && r.IsSuccess) { return r.GetValue(); } return 0; }")]
    internal async Task A_check_written_in_parentheses_or_as_one_operand_of_an_and_or_an_or_guards_the_access(string method)
    {
        var ids = await AnalyzerTestHost.AnalyzeAsync(method);

        Assert.Empty(ids);
    }

    [Theory]
    [InlineData("int M(Result<int> r, bool other) { if (r.IsSuccess && other) { return 1; } else { return r.GetValue(); } }")]
    [InlineData("int M(Result<int> r, bool other) { if (r.IsFailure || other) { return r.GetValue(); } return 0; }")]
    [InlineData("int M(Result<int> r, Result<int> other) { if (r.Equals(other)) { return r.GetValue(); } return 0; }")]
    [InlineData("int M(Result<int> r, bool a, bool b) { if (a && b) { return r.GetValue(); } return 0; }")]
    [InlineData("int M(Result<int> r, bool a, bool b) { if (a || b) { return 0; } return r.GetValue(); }")]
    internal async Task A_check_that_says_nothing_about_the_state_in_the_branch_does_not_guard_the_access(string method)
    {
        var ids = await AnalyzerTestHost.AnalyzeAsync(method);

        Assert.Equal(["WYZ0102"], ids);
    }

    [Fact]
    internal async Task An_unused_value_that_is_not_a_result_is_not_reported_even_when_it_is_an_array()
    {
        var ids = await AnalyzerTestHost.AnalyzeAsync("static int[] MakeArray() => []; void M() { MakeArray(); }");

        Assert.Empty(ids);
    }

    [Fact]
    internal async Task An_awaited_variable_that_is_not_used_is_reported_with_its_own_text()
    {
        var ids = await AnalyzerTestHost.AnalyzeAsync("async Task M(Task<Result<int>> task) { await task; }");

        Assert.Equal(["WYZ0101"], ids);
    }

    [Fact]
    internal async Task The_analyzers_do_nothing_for_a_compilation_that_does_not_know_the_library()
    {
        var usage = await AnalyzerTestHost.AnalyzeWithoutTheLibraryAsync(new ResultUsageAnalyzer());
        var suggestions = await AnalyzerTestHost.AnalyzeWithoutTheLibraryAsync(Suggestions);

        Assert.Multiple(
            () => Assert.Empty(usage),
            () => Assert.Empty(suggestions));
    }

    [Theory]
    [InlineData("WYZ0106", "Result<string> M(Result<string?> r) => r.Bind(x => null == x ? Result.Failure<string>(Rejected) : Result.Success(x));")]
    [InlineData("WYZ0106", "Result<string> M(Result<string?> r) => r.Bind(x => null != x ? Result.Success(x) : Result.Failure<string>(Rejected));")]
    [InlineData("WYZ0112", "Result<int> M(Result<int> r) => r.Bind(x => (Result.Success(x * 2)));")]
    internal async Task A_suggestion_written_with_null_on_the_left_or_with_parentheses_is_reported(string id, string method)
    {
        var ids = await AnalyzerTestHost.AnalyzeAsync(Sources + "\n" + method, Suggestions);

        Assert.Equal([id], ids);
    }

    [Theory]
    [InlineData("Result<int> M(Result<(int, int)> r) => r.Map((a, b) => a + b);")]
    [InlineData("Result<int> M(Result<int> r) => r.Map<int, int>(x => x);")]
    [InlineData("Result<int>? M(Result<int>? r) => r?.Map(x => x);")]
    [InlineData("Result<int> M(Result<int> r) => r.Map(n => { n = n + 1; return n; });")]
    [InlineData("Result<int> M(Result<int> r) => r.Map(n => { Bump(ref n); return n; });")]
    [InlineData("Result<string> M(Result<string?> r) => r.Bind(x => x is \"a\" ? Result.Failure<string>(Rejected) : Result.Success(x!));")]
    [InlineData("Result<string> M(Result<string?> r) => r.Bind(x => x == \"a\" ? Result.Failure<string>(Rejected) : Result.Success(x!));")]
    [InlineData("Result<string> M(Result<string?> r) => r.Bind(x => \"a\" == x ? Result.Failure<string>(Rejected) : Result.Success(x!));")]
    [InlineData("Result<string> M(Result<string?> r, string? other) => r.Bind(x => x == other ? Result.Failure<string>(Rejected) : Result.Success(x!));")]
    [InlineData("Result<T> M<T>(Result<T> r, Error error) => r.Bind(x => x is null ? Result.Failure<T>(error) : Result.Success(x));")]
    [InlineData("Result M(bool ok, Result existing) => ok ? Result.Success() : existing;")]
    [InlineData("Result M(bool ok) => ok ? Result.Success() : Fail(Rejected);")]
    internal async Task Shapes_that_only_look_like_a_suggestion_are_left_alone(string method)
    {
        var ids = await AnalyzerTestHost.AnalyzeAsync(Sources + "\n" + method, Suggestions);

        Assert.Empty(ids);
    }
}
