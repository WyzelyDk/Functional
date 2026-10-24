using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Wyzely.Functional.AnalyzerTesting;
using Wyzely.Functional.MaybeAnalyzers;

namespace Wyzely.Functional.MaybeAnalyzersTests;

// Runs the analyzer over a snippet, offers its first diagnostic to a code fix provider inside a real Roslyn workspace, and
// applies the action that the provider offers.
internal static class CodeFixTestHost
{
    internal static Task<CodeFixOutcome> FixAsync(string members, CodeFixProvider provider, string diagnosticId) =>
        FixAsync(members, provider, diagnosticId, new MaybeUsageAnalyzer());

    internal static Task<CodeFixOutcome> FixAsync(string members, CodeFixProvider provider, string diagnosticId, DiagnosticAnalyzer analyzer) =>
        CodeFixTesting.FixAsync(
            AnalyzerTestHost.Header + "public class Sample\n{\n" + members + "\n}\n",
            AnalyzerTestHost.References(),
            provider,
            diagnosticId,
            analyzer,
            TestContext.Current.CancellationToken);
}
