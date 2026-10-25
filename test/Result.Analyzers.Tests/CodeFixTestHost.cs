using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Wyzely.Functional.AnalyzerTesting;
using Wyzely.Functional.ResultAnalyzers;

namespace Wyzely.Functional.ResultAnalyzersTests;

// Runs the analyzer over a snippet, offers its first diagnostic to a code fix provider inside a real Roslyn workspace, and
// applies the action that the provider offers.
internal static class CodeFixTestHost
{
    internal static Task<CodeFixOutcome> FixAsync(string members, CodeFixProvider provider, string diagnosticId) =>
        FixAsync(members, provider, diagnosticId, new ResultUsageAnalyzer());

    internal static Task<CodeFixOutcome> FixAsync(string members, CodeFixProvider provider, string diagnosticId, DiagnosticAnalyzer analyzer) =>
        CodeFixTesting.FixAsync(
            AnalyzerTestHost.Header + "public class Sample\n{\n" + members + "\n}\n",
            AnalyzerTestHost.References(),
            provider,
            diagnosticId,
            analyzer,
            TestContext.Current.CancellationToken);

    // How many fixes the provider offers for a diagnostic that is made up and reported at the marker.
    internal static Task<int> OfferedFixesAsync(string members, CodeFixProvider provider, string diagnosticId, string marker) =>
        CodeFixTesting.OfferedFixesAsync(
            AnalyzerTestHost.Header + "public class Sample\n{\n" + members + "\n}\n",
            AnalyzerTestHost.References(),
            provider,
            diagnosticId,
            marker,
            TestContext.Current.CancellationToken);
}
