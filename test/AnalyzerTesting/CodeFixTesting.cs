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
    internal static ImmutableArray<MetadataReference> References(string libraryAssemblyPath)
    {
        var trusted = (string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!;
        var paths = trusted.Split(Path.PathSeparator).ToList();
        paths.Add(libraryAssemblyPath);

        return [.. paths.Distinct(StringComparer.OrdinalIgnoreCase).Select(path => (MetadataReference)MetadataReference.CreateFromFile(path))];
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
}
