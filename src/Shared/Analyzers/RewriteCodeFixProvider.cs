using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Formatting;

namespace Wyzely.Functional;

/// <summary>
/// The part that the suggestion code fixes of <c>Result</c> and <c>Maybe</c> have in common: find the node a diagnostic is about, rewrite it, and put the
/// rewritten node, formatted, in its place. A provider names its diagnostics and says what a target is and how to rewrite it. The file is compiled into
/// each code fix project as source, so no shared assembly has to ship; the class has to be public because the providers that derive from it are.
/// </summary>
public abstract class RewriteCodeFixProvider : CodeFixProvider
{
    /// <inheritdoc />
    public sealed override ImmutableArray<string> FixableDiagnosticIds => [.. Titles.Keys.OrderBy(id => id, StringComparer.Ordinal)];

    /// <summary>Gets the title of the fix for each diagnostic id that the provider fixes.</summary>
    protected abstract ImmutableDictionary<string, string> Titles { get; }

    /// <inheritdoc />
    public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    /// <inheritdoc />
    public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        var diagnostic = context.Diagnostics[0];

        if (root is null || !Titles.TryGetValue(diagnostic.Id, out var title))
        {
            return;
        }

        var node = root.FindNode(diagnostic.Location.SourceSpan, getInnermostNodeForTie: true);
        var target = node.AncestorsAndSelf().FirstOrDefault(candidate => IsTarget(candidate, diagnostic.Id));

        if (target is null || Rewrite(target, diagnostic) is null)
        {
            return;
        }

        context.RegisterCodeFix(
            CodeAction.Create(title, token => ApplyAsync(context.Document, target, diagnostic, token), equivalenceKey: diagnostic.Id),
            diagnostic);
    }

    /// <summary>Determines whether a node is the one to rewrite for a diagnostic, searching from the node at its location outwards.</summary>
    /// <param name="node">The node to test.</param>
    /// <param name="id">The id of the diagnostic.</param>
    /// <returns><see langword="true"/> for the node to rewrite.</returns>
    protected abstract bool IsTarget(SyntaxNode node, string id);

    /// <summary>Rewrites the node that a diagnostic is about.</summary>
    /// <param name="target">The node to rewrite.</param>
    /// <param name="diagnostic">The diagnostic, with the properties that the analyzer put on it.</param>
    /// <returns>The node to put in its place, or <see langword="null"/> when the code does not have the expected shape.</returns>
    protected abstract SyntaxNode? Rewrite(SyntaxNode target, Diagnostic diagnostic);

    private async Task<Document> ApplyAsync(Document document, SyntaxNode target, Diagnostic diagnostic, CancellationToken token)
    {
        var root = await document.GetSyntaxRootAsync(token).ConfigureAwait(false);
        var replacement = Rewrite(target, diagnostic)!.WithTriviaFrom(target).WithAdditionalAnnotations(Formatter.Annotation);

        return document.WithSyntaxRoot(root!.ReplaceNode(target, replacement));
    }
}
