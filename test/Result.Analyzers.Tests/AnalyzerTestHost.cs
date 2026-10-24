using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

using Wyzely.Functional.AnalyzerTesting;
using Wyzely.Functional.ResultAnalyzers;

namespace Wyzely.Functional.ResultAnalyzersTests;

// Compiles a snippet against the real library and runs the analyzer over it.
internal static class AnalyzerTestHost
{
    internal const string Header = """
        #nullable enable
        using System;
        using System.Collections.Generic;
        using System.Threading.Tasks;
        using Wyzely.Functional;

        """;

    // Returns the ids of the diagnostics that the analyzer reports for the members of a class.
    internal static Task<string[]> AnalyzeAsync(string members) => AnalyzeAsync(members, new ResultUsageAnalyzer());

    // Returns the ids of the diagnostics that the given analyzer reports for the members of a class.
    internal static async Task<string[]> AnalyzeAsync(string members, DiagnosticAnalyzer analyzer)
    {
        var compilation = Compile(members);
        var withAnalyzers = compilation.WithAnalyzers([analyzer]);
        var diagnostics = await withAnalyzers.GetAnalyzerDiagnosticsAsync(TestContext.Current.CancellationToken);

        return [.. diagnostics.Select(diagnostic => diagnostic.Id).OrderBy(id => id, StringComparer.Ordinal)];
    }

    // Returns the ids of the nullable-reference-type warnings that the compiler reports for the members of a class.
    internal static string[] NullabilityWarnings(string members)
    {
        var compilation = Compile(members);
        var warnings = compilation.GetDiagnostics()
            .Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Warning && diagnostic.Id.StartsWith("CS86", StringComparison.Ordinal))
            .Select(diagnostic => diagnostic.Id)
            .OrderBy(id => id, StringComparer.Ordinal);

        return [.. warnings];
    }

    internal static ImmutableArray<MetadataReference> References() => CodeFixTesting.References(typeof(Result).Assembly.Location);

    private static CSharpCompilation Compile(string members) =>
        CodeFixTesting.Compile(Header + "public class Sample\n{\n" + members + "\n}\n", References());
}
