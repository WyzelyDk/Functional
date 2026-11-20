using Wyzely.Functional.AnalyzerTesting;

namespace Wyzely.Functional.MaybeAnalyzersTests;

// The analyzers and the code fixes of the package must not reference a newer Roslyn than the oldest compiler that the package supports.
public sealed class AnalyzerRoslynVersionTests
{
    [Fact]
    internal void The_analyzers_do_not_reference_a_newer_Roslyn_than_the_oldest_supported_compiler()
    {
        var assembly = typeof(Wyzely.Functional.MaybeAnalyzers.MaybeUsageAnalyzer).Assembly;

        Assert.Multiple(
            () => Assert.True(RoslynReferences.Count(assembly) > 0),
            () => Assert.Empty(RoslynReferences.NewerThan(assembly, RoslynReferences.OldestSupported())));
    }

    [Fact]
    internal void The_code_fixes_do_not_reference_a_newer_Roslyn_than_the_oldest_supported_compiler()
    {
        var assembly = typeof(Wyzely.Functional.MaybeAnalyzers.MaybeSuggestionsCodeFixProvider).Assembly;

        Assert.Multiple(
            () => Assert.True(RoslynReferences.Count(assembly) > 0),
            () => Assert.Empty(RoslynReferences.NewerThan(assembly, RoslynReferences.OldestSupported())));
    }
}
