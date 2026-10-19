using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

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
    internal static async Task<string[]> AnalyzeAsync(string members)
    {
        var compilation = Compile(members);
        var withAnalyzers = compilation.WithAnalyzers([new ResultUsageAnalyzer()]);
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

    internal static ImmutableArray<MetadataReference> References()
    {
        var trusted = (string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!;
        var paths = trusted.Split(Path.PathSeparator).ToList();
        paths.Add(typeof(Result).Assembly.Location);

        return [.. paths.Distinct(StringComparer.OrdinalIgnoreCase).Select(path => (MetadataReference)MetadataReference.CreateFromFile(path))];
    }

    private static CSharpCompilation Compile(string members)
    {
        var source = Header + "public class Sample\n{\n" + members + "\n}\n";
        var tree = CSharpSyntaxTree.ParseText(source);
        var compilation = CSharpCompilation.Create(
            "Sample",
            [tree],
            References(),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));

        var errors = compilation.GetDiagnostics().Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).ToList();
        Assert.True(errors.Count == 0, "The snippet does not compile: " + string.Join("; ", errors));

        return compilation;
    }
}
