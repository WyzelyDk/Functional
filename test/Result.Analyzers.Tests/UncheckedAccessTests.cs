namespace Wyzely.Functional.ResultAnalyzersTests;

public sealed class UncheckedAccessTests
{
    private const string Sources = """
        static Result<int> Compute() => Result.Success(1);
        static List<int> Values = new();
        """;

    [Fact]
    internal async Task GetValue_without_a_check_is_reported()
    {
        var ids = await AnalyzerTestHost.AnalyzeAsync(Sources + "\nint M(Result<int> r) => r.GetValue();");

        Assert.Equal(["WYZ0102"], ids);
    }

    [Fact]
    internal async Task GetError_without_a_check_is_reported()
    {
        var ids = await AnalyzerTestHost.AnalyzeAsync(Sources + "\nError M(Result r) => r.GetError();");

        Assert.Equal(["WYZ0102"], ids);
    }

    [Fact]
    internal async Task GetError_of_a_typed_result_without_a_check_is_reported()
    {
        var ids = await AnalyzerTestHost.AnalyzeAsync(Sources + "\nError M(Result<int> r) => r.GetError();");

        Assert.Equal(["WYZ0102"], ids);
    }

    [Fact]
    internal async Task An_access_on_the_result_of_a_call_cannot_be_guarded_and_is_reported()
    {
        var ids = await AnalyzerTestHost.AnalyzeAsync(Sources + "\nint M() => Compute().GetValue();");

        Assert.Equal(["WYZ0102"], ids);
    }

    [Fact]
    internal async Task GetValue_inside_an_if_on_IsSuccess_is_not_reported()
    {
        var ids = await AnalyzerTestHost.AnalyzeAsync(Sources + "\nint M(Result<int> r) { if (r.IsSuccess) { return r.GetValue(); } return 0; }");

        Assert.Empty(ids);
    }

    [Fact]
    internal async Task GetError_inside_an_if_on_IsFailure_is_not_reported()
    {
        var ids = await AnalyzerTestHost.AnalyzeAsync(Sources + "\nError? M(Result r) { if (r.IsFailure) { return r.GetError(); } return null; }");

        Assert.Empty(ids);
    }

    [Fact]
    internal async Task GetValue_after_an_early_exit_on_IsFailure_is_not_reported()
    {
        var ids = await AnalyzerTestHost.AnalyzeAsync(Sources + "\nint M(Result<int> r) { if (r.IsFailure) { return 0; } return r.GetValue(); }");

        Assert.Empty(ids);
    }

    [Fact]
    internal async Task GetError_after_an_early_exit_on_IsSuccess_is_not_reported()
    {
        var ids = await AnalyzerTestHost.AnalyzeAsync(Sources + "\nError M(Result r) { if (r.IsSuccess) throw new InvalidOperationException(); " +
            "return r.GetError(); }");

        Assert.Empty(ids);
    }

    [Fact]
    internal async Task GetValue_after_a_continue_on_IsFailure_is_not_reported()
    {
        var ids = await AnalyzerTestHost.AnalyzeAsync(
            Sources + "\nvoid M(List<Result<int>> rs) { foreach (var r in rs) { if (r.IsFailure) { continue; } Values.Add(r.GetValue()); } }");

        Assert.Empty(ids);
    }

    [Fact]
    internal async Task GetError_in_the_true_branch_of_a_conditional_on_IsFailure_is_not_reported()
    {
        var ids = await AnalyzerTestHost.AnalyzeAsync(Sources + "\nError? M(Result r) => r.IsFailure ? r.GetError() : null;");

        Assert.Empty(ids);
    }

    [Fact]
    internal async Task GetError_in_the_false_branch_of_a_conditional_on_IsSuccess_is_not_reported()
    {
        var ids = await AnalyzerTestHost.AnalyzeAsync(Sources + "\nError? M(Result r) => r.IsSuccess ? null : r.GetError();");

        Assert.Empty(ids);
    }

    [Fact]
    internal async Task GetError_to_the_right_of_an_and_on_IsFailure_is_not_reported()
    {
        var ids = await AnalyzerTestHost.AnalyzeAsync(Sources + "\nbool M(Result r) => r.IsFailure && r.GetError().Message.Length > 0;");

        Assert.Empty(ids);
    }

    [Fact]
    internal async Task GetError_to_the_right_of_an_or_on_IsSuccess_is_not_reported()
    {
        var ids = await AnalyzerTestHost.AnalyzeAsync(Sources + "\nbool M(Result r) => r.IsSuccess || r.GetError().Message.Length > 0;");

        Assert.Empty(ids);
    }

    [Fact]
    internal async Task GetError_after_a_negated_IsSuccess_is_not_reported()
    {
        var ids = await AnalyzerTestHost.AnalyzeAsync(Sources + "\nError? M(Result r) { if (!r.IsSuccess) { return r.GetError(); } return null; }");

        Assert.Empty(ids);
    }

    [Fact]
    internal async Task GetError_in_the_else_branch_of_an_if_on_IsSuccess_is_not_reported()
    {
        var ids = await AnalyzerTestHost.AnalyzeAsync(Sources + "\nError? M(Result r) { if (r.IsSuccess) { return null; } else { return r.GetError(); } }");

        Assert.Empty(ids);
    }

    [Fact]
    internal async Task GetValue_after_a_successful_TryGetValue_is_not_reported()
    {
        var ids = await AnalyzerTestHost.AnalyzeAsync(Sources + "\nint M(Result<int> r) { if (r.TryGetValue(out _)) { return r.GetValue(); } return 0; }");

        Assert.Empty(ids);
    }

    [Fact]
    internal async Task The_wrong_check_does_not_guard_the_access()
    {
        var ids = await AnalyzerTestHost.AnalyzeAsync(
            Sources + "\nError M(Result r) { if (r.IsSuccess) { return r.GetError(); } return Error.Create(\"x\"); }");

        Assert.Equal(["WYZ0102"], ids);
    }

    [Fact]
    internal async Task A_check_of_another_result_does_not_guard_the_access()
    {
        var ids = await AnalyzerTestHost.AnalyzeAsync(Sources + "\nint M(Result<int> a, Result<int> b) { if (a.IsSuccess) { return b.GetValue(); } " +
            "return 0; }");

        Assert.Equal(["WYZ0102"], ids);
    }

    [Fact]
    internal async Task A_check_that_does_not_exit_does_not_guard_the_access()
    {
        var ids = await AnalyzerTestHost.AnalyzeAsync(Sources + "\nint M(Result<int> r) { if (r.IsFailure) { Values.Clear(); } return r.GetValue(); }");

        Assert.Equal(["WYZ0102"], ids);
    }

    [Fact]
    internal async Task A_method_with_the_same_name_on_another_type_is_not_reported()
    {
        var ids = await AnalyzerTestHost.AnalyzeAsync(
            """
            sealed class Other { public int GetValue() => 1; }
            int M(Other other) => other.GetValue();
            """);

        Assert.Empty(ids);
    }
}
