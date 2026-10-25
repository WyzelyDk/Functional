using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace Wyzely.Functional.AnalyzerTesting;

// What the analyzer test projects of Result and Maybe share, compiled into each of them as source: the references that a snippet compiles against,
// and the run of an analyzer and a code fix provider over a snippet inside a real Roslyn workspace.
internal static class CodeFixTesting
{
    // The references of the running platform and of the library under test.
    internal static ImmutableArray<MetadataReference> References(string libraryAssemblyPath) => References(PlatformPaths().Append(libraryAssemblyPath));

    // The references of the running platform alone: a compilation that does not know the library.
    internal static ImmutableArray<MetadataReference> PlatformReferences() =>
        References(PlatformPaths().Where(static path => !Path.GetFileName(path).StartsWith("Wyzely.", StringComparison.Ordinal)));

    // Runs the analyzer over the compilation and returns the ids of what it reports, in order.
    internal static async Task<string[]> AnalyzeAsync(CSharpCompilation compilation, DiagnosticAnalyzer analyzer, CancellationToken token)
    {
        var diagnostics = await compilation.WithAnalyzers([analyzer]).GetAnalyzerDiagnosticsAsync(token);

        return [.. diagnostics.Select(diagnostic => diagnostic.Id).OrderBy(id => id, StringComparer.Ordinal)];
    }

    // Asks the provider for a fix of a diagnostic that is made up: the given id, reported at the first occurrence of the marker in the source. It
    // returns how many fixes the provider offered, which shows that it does not offer one for a diagnostic that is stale or does not fit the code.
    internal static async Task<int> OfferedFixesAsync(
        string source,
        ImmutableArray<MetadataReference> references,
        CodeFixProvider provider,
        string diagnosticId,
        string marker,
        CancellationToken token)
    {
        using var workspace = new AdhocWorkspace();
        var projectId = ProjectId.CreateNewId();
        var projectInfo = ProjectInfo.Create(
            projectId,
            VersionStamp.Create(),
            "Sample",
            "Sample",
            LanguageNames.CSharp,
            compilationOptions: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable),
            metadataReferences: references);
        var document = workspace.CurrentSolution
            .AddProject(projectInfo)
            .AddDocument(DocumentId.CreateNewId(projectId), "Sample.cs", SourceText.From(source))
            .Projects.Single().Documents.Single();
        var tree = (await document.GetSyntaxTreeAsync(token))!;
        var start = source.IndexOf(marker, StringComparison.Ordinal);
        Assert.True(start >= 0, "The marker is not in the source: " + marker);
        var descriptor = new DiagnosticDescriptor(diagnosticId, "Made up", "Made up", "Test", DiagnosticSeverity.Warning, isEnabledByDefault: true);
        var diagnostic = Diagnostic.Create(descriptor, Location.Create(tree, new TextSpan(start, marker.Length)));
        var actions = new List<CodeAction>();

        await provider.RegisterCodeFixesAsync(new CodeFixContext(document, diagnostic, (action, _) => actions.Add(action), token));

        return actions.Count;
    }

    // Compiles a source against the references, and fails the test when the source does not compile.
    internal static CSharpCompilation Compile(string source, ImmutableArray<MetadataReference> references)
    {
        var compilation = CSharpCompilation.Create(
            "Sample",
            [CSharpSyntaxTree.ParseText(source)],
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));

        var errors = compilation.GetDiagnostics().Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).ToList();
        Assert.True(errors.Count == 0, "The snippet does not compile: " + string.Join("; ", errors));

        return compilation;
    }

    // Runs the analyzer over the source, offers its first diagnostic with the given id to the provider, and applies the action that the provider offers.
    internal static async Task<CodeFixOutcome> FixAsync(
        string source,
        ImmutableArray<MetadataReference> references,
        CodeFixProvider provider,
        string diagnosticId,
        DiagnosticAnalyzer analyzer,
        CancellationToken token)
    {
        using var workspace = new AdhocWorkspace();
        var projectId = ProjectId.CreateNewId();
        var projectInfo = ProjectInfo.Create(
            projectId,
            VersionStamp.Create(),
            "Sample",
            "Sample",
            LanguageNames.CSharp,
            compilationOptions: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable),
            metadataReferences: references);
        var solution = workspace.CurrentSolution
            .AddProject(projectInfo)
            .AddDocument(DocumentId.CreateNewId(projectId), "Sample.cs", SourceText.From(source));
        var document = solution.Projects.Single().Documents.Single();

        var diagnostics = await DiagnosticsAsync(document, analyzer, token);
        var diagnostic = diagnostics.First(candidate => candidate.Id == diagnosticId);
        var actions = new List<CodeAction>();
        await provider.RegisterCodeFixesAsync(new CodeFixContext(document, diagnostic, (action, _) => actions.Add(action), token));

        if (actions.Count == 0)
        {
            return new CodeFixOutcome(null, [.. diagnostics.Select(candidate => candidate.Id)]);
        }

        var operations = await actions[0].GetOperationsAsync(token);
        var changed = operations.OfType<ApplyChangesOperation>().Single().ChangedSolution.GetDocument(document.Id)!;
        var remaining = await DiagnosticsAsync(changed, analyzer, token);

        return new CodeFixOutcome((await changed.GetTextAsync(token)).ToString(), [.. remaining.Select(candidate => candidate.Id)]);
    }

    private static async Task<IReadOnlyList<Diagnostic>> DiagnosticsAsync(Document document, DiagnosticAnalyzer analyzer, CancellationToken token)
    {
        var compilation = (await document.Project.GetCompilationAsync(token))!;
        var withAnalyzers = compilation.WithAnalyzers([analyzer]);

        return await withAnalyzers.GetAnalyzerDiagnosticsAsync(token);
    }

    private static string[] PlatformPaths() => ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator);

    private static ImmutableArray<MetadataReference> References(IEnumerable<string> paths) =>
        [.. paths.Distinct(StringComparer.OrdinalIgnoreCase).Select(path => (MetadataReference)MetadataReference.CreateFromFile(path))];
}
