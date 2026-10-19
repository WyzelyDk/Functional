using Wyzely.Functional.ResultAnalyzers;

namespace Wyzely.Functional.ResultAnalyzersTests;

public sealed class UnusedResultCodeFixTests
{
    private const string Sources = """
        static Result Run() => Result.Success();
        static Result<int> Compute() => Result.Success(1);
        static Task<Result> RunAsync() => Task.FromResult(Result.Success());
        """;

    private static readonly UnusedResultCodeFixProvider Provider = new();

    [Theory]
    [InlineData("void M() { Run(); }", "_ = Run();")]
    [InlineData("void M() { Compute(); }", "_ = Compute();")]
    [InlineData("async Task M() { await RunAsync(); }", "_ = await RunAsync();")]
    [InlineData("void M() { RunAsync(); }", "_ = RunAsync();")]
    internal async Task An_unused_result_is_discarded_explicitly(string method, string expected)
    {
        var outcome = await CodeFixTestHost.FixAsync(Sources + "\n" + method, Provider, "WYZ0101");

        Assert.Multiple(
            () => Assert.NotNull(outcome.FixedSource),
            () => Assert.Contains(expected, outcome.FixedSource),
            () => Assert.DoesNotContain("WYZ0101", outcome.RemainingIds));
    }

    [Fact]
    internal void The_provider_fixes_only_WYZ0101_and_supports_fixing_all_occurrences()
    {
        Assert.Multiple(
            () => Assert.Equal(["WYZ0101"], Provider.FixableDiagnosticIds),
            () => Assert.NotNull(Provider.GetFixAllProvider()));
    }
}
