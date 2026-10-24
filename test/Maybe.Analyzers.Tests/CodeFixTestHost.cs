using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

using Wyzely.Functional.MaybeAnalyzers;

namespace Wyzely.Functional.MaybeAnalyzersTests;

// Runs the analyzer over a snippet, offers its first diagnostic to a code fix provider inside a real Roslyn workspace, and
// applies the action that the provider offers.
internal static class CodeFixTestHost
{
    internal static Task<CodeFixOutcome> FixAsync(string members, CodeFixProvider provider, string diagnosticId) =>
        FixAsync(members, provider, diagnosticId, new MaybeUsageAnalyzer());

    internal static async Task<CodeFixOutcome> FixAsync(string members, CodeFixProvider provider, string diagnosticId, DiagnosticAnalyzer analyzer)
    {
        var token = TestContext.Current.CancellationToken;
        var source = AnalyzerTestHost.Header + "public class Sample\n{\n" + members + "\n}\n";
        using var workspace = new AdhocWorkspace();
        var projectId = ProjectId.CreateNewId();
        var projectInfo = ProjectInfo.Create(
            projectId,
            VersionStamp.Create(),
            "Sample",
            "Sample",
            LanguageNames.CSharp,
            compilationOptions: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable),
            metadataReferences: AnalyzerTestHost.References());
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
