using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Wyzely.Functional.ResultAnalyzers;

/// <summary>
/// Offers to discard an unused result explicitly with <c>_ =</c> (WYZ0101), which states that ignoring the outcome is intended.
/// The fix does not handle the result; it only makes the decision to ignore it visible.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(UnusedResultCodeFixProvider))]
[System.Composition.Shared]
public sealed class UnusedResultCodeFixProvider : CodeFixProvider
{
    private const string Title = "Discard the result explicitly";

    /// <inheritdoc />
    public override ImmutableArray<string> FixableDiagnosticIds { get; } = ImmutableArray.Create("WYZ0101");

    /// <inheritdoc />
    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    /// <inheritdoc />
    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        var diagnostic = context.Diagnostics[0];
        var statement = root?.FindNode(diagnostic.Location.SourceSpan, getInnermostNodeForTie: true).FirstAncestorOrSelf<ExpressionStatementSyntax>();

        if (statement is null)
        {
            return;
        }

        context.RegisterCodeFix(
            CodeAction.Create(Title, token => DiscardAsync(context.Document, statement, token), equivalenceKey: Title),
            diagnostic);
    }

    private static async Task<Document> DiscardAsync(Document document, ExpressionStatementSyntax statement, CancellationToken token)
    {
        var root = await document.GetSyntaxRootAsync(token).ConfigureAwait(false);
        var expression = statement.Expression;
        var discard = SyntaxFactory.AssignmentExpression(
            SyntaxKind.SimpleAssignmentExpression,
            SyntaxFactory.IdentifierName("_"),
            expression.WithoutLeadingTrivia())
            .WithLeadingTrivia(expression.GetLeadingTrivia());

        return document.WithSyntaxRoot(root!.ReplaceNode(statement, statement.WithExpression(discard)));
    }
}
