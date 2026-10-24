using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Formatting;

namespace Wyzely.Functional;

/// <summary>
/// The part that the suggestion code fixes of <c>Result</c> and <c>Maybe</c> have in common: find the node a diagnostic is about, rewrite it, and put the
/// rewritten node, formatted, in its place. The file is compiled into each code fix project as internal source.
/// </summary>
internal static class RewriteFixes
{
    /// <summary>Offers the fix of a diagnostic, when the node it points at can be rewritten.</summary>
    /// <param name="context">The context of the code fix provider.</param>
    /// <param name="titles">The title of the fix for each diagnostic id that the provider fixes.</param>
    /// <param name="targetOf">Finds the node to rewrite, from the node at the location of the diagnostic and the id of the diagnostic.</param>
    /// <param name="rewrite">Rewrites the node, or returns <see langword="null"/> when the code does not have the expected shape.</param>
    /// <returns>A task that completes when the fix has been registered, or when there was nothing to register.</returns>
    internal static async Task RegisterAsync(
        CodeFixContext context,
        ImmutableDictionary<string, string> titles,
        Func<SyntaxNode, string, SyntaxNode?> targetOf,
        Func<SyntaxNode, Diagnostic, SyntaxNode?> rewrite)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        var diagnostic = context.Diagnostics[0];

        if (root is null || !titles.TryGetValue(diagnostic.Id, out var title))
        {
            return;
        }

        var target = targetOf(root.FindNode(diagnostic.Location.SourceSpan, getInnermostNodeForTie: true), diagnostic.Id);

        if (target is null || rewrite(target, diagnostic) is null)
        {
            return;
        }

        context.RegisterCodeFix(
            CodeAction.Create(title, token => ApplyAsync(context.Document, target, diagnostic, rewrite, token), equivalenceKey: diagnostic.Id),
            diagnostic);
    }

    private static async Task<Document> ApplyAsync(
        Document document,
        SyntaxNode target,
        Diagnostic diagnostic,
        Func<SyntaxNode, Diagnostic, SyntaxNode?> rewrite,
        CancellationToken token)
    {
        var root = await document.GetSyntaxRootAsync(token).ConfigureAwait(false);
        var replacement = rewrite(target, diagnostic)!.WithTriviaFrom(target).WithAdditionalAnnotations(Formatter.Annotation);

        return document.WithSyntaxRoot(root!.ReplaceNode(target, replacement));
    }
}
