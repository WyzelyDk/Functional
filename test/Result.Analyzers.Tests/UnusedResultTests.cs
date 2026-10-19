namespace Wyzely.Functional.ResultAnalyzersTests;

public sealed class UnusedResultTests
{
    private const string Sources = """
        static Result Run() => Result.Success();
        static Result<int> Compute() => Result.Success(1);
        static Task<Result> RunAsync() => Task.FromResult(Result.Success());
        static ValueTask<Result<int>> ComputeAsync() => new(Result.Success(1));
        static Task PlainAsync() => Task.CompletedTask;
        """;

    [Fact]
    internal async Task A_discarded_result_is_reported()
    {
        var ids = await AnalyzerTestHost.AnalyzeAsync(Sources + "\nvoid M() { Run(); }");

        Assert.Equal(["WYZ0101"], ids);
    }

    [Fact]
    internal async Task A_discarded_typed_result_is_reported()
    {
        var ids = await AnalyzerTestHost.AnalyzeAsync(Sources + "\nvoid M() { Compute(); }");

        Assert.Equal(["WYZ0101"], ids);
    }

    [Fact]
    internal async Task A_result_discarded_after_await_is_reported()
    {
        var ids = await AnalyzerTestHost.AnalyzeAsync(Sources + "\nasync Task M() { await RunAsync(); await ComputeAsync(); }");

        Assert.Equal(["WYZ0101", "WYZ0101"], ids);
    }

    [Fact]
    internal async Task A_task_of_a_result_that_is_neither_awaited_nor_used_is_reported()
    {
        var ids = await AnalyzerTestHost.AnalyzeAsync(Sources + "\nvoid M() { RunAsync(); ComputeAsync(); }");

        Assert.Equal(["WYZ0101", "WYZ0101"], ids);
    }

    [Fact]
    internal async Task A_result_that_is_explicitly_discarded_is_not_reported()
    {
        var ids = await AnalyzerTestHost.AnalyzeAsync(Sources + "\nvoid M() { _ = Run(); _ = Compute(); }");

        Assert.Empty(ids);
    }

    [Fact]
    internal async Task A_result_that_is_assigned_or_returned_is_not_reported()
    {
        var ids = await AnalyzerTestHost.AnalyzeAsync(Sources + "\nResult M() { var a = Compute(); _ = a; return Run(); }");

        Assert.Empty(ids);
    }

    [Fact]
    internal async Task A_result_that_is_used_further_is_not_reported()
    {
        var ids = await AnalyzerTestHost.AnalyzeAsync(Sources + "\nvoid M() { Run().ThrowIfFailure(); }");

        Assert.Empty(ids);
    }

    [Fact]
    internal async Task A_plain_task_that_is_not_awaited_is_not_reported_by_this_rule()
    {
        var ids = await AnalyzerTestHost.AnalyzeAsync(Sources + "\nvoid M() { PlainAsync(); }");

        Assert.Empty(ids);
    }
}
