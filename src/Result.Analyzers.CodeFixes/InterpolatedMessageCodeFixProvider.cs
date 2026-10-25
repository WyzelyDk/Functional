using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;

namespace Wyzely.Functional.ResultAnalyzers;

/// <summary>Moves the values of an interpolated error message into details of the error.</summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(InterpolatedMessageCodeFixProvider))]
[System.Composition.Shared]
public sealed class InterpolatedMessageCodeFixProvider : CodeFixProvider
{
    /// <inheritdoc />
    public override ImmutableArray<string> FixableDiagnosticIds { get; } = ["WYZ0111"];

    /// <inheritdoc />
    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    /// <inheritdoc />
    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        var diagnostic = context.Diagnostics[0];
        var interpolated = root!.FindNode(diagnostic.Location.SourceSpan, getInnermostNodeForTie: true)
            .AncestorsAndSelf()
            .OfType<InterpolatedStringExpressionSyntax>()
            .FirstOrDefault();

        if (interpolated is null || Rewrite(interpolated) is null)
        {
            return;
        }

        context.RegisterCodeFix(
            CodeAction.Create("Make the values details of the error", token => ApplyAsync(context.Document, interpolated, token), equivalenceKey: "WYZ0111"),
            diagnostic);
    }

    private static async Task<Document> ApplyAsync(Document document, InterpolatedStringExpressionSyntax interpolated, CancellationToken token)
    {
        var root = await document.GetSyntaxRootAsync(token).ConfigureAwait(false);
        var call = (InvocationExpressionSyntax)interpolated.Parent!.Parent!.Parent!;
        var replacement = Rewrite(interpolated)!.WithTriviaFrom(call).WithAdditionalAnnotations(Formatter.Annotation);

        return document.WithSyntaxRoot(root!.ReplaceNode(call, replacement));
    }

    // The call that has the interpolated string as its message, written again with the values as details, or null when the fix cannot keep the message.
    private static ExpressionSyntax? Rewrite(InterpolatedStringExpressionSyntax interpolated)
    {
        if (interpolated is not { Parent: ArgumentSyntax { NameColon: null } argument }
            || argument.Parent!.Parent is not InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax access } call
            || !TryRead(interpolated, out var template, out var details))
        {
            return null;
        }

        var name = access.Name.Identifier.ValueText;
        var receiver = access.Expression is IdentifierNameSyntax identifier ? identifier.Identifier.ValueText : null;

        if (name == "WithContext")
        {
            return call.WithArgumentList(SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(ContextArguments(template, details))));
        }

        if (receiver == "Error" && call.ArgumentList.Arguments[0] == argument)
        {
            return AddDetails(ReplaceArgument(call, argument, template), details);
        }

        if (receiver == "Result" && name == "Failure" && call.ArgumentList.Arguments.Count == 1)
        {
            var error = SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.IdentifierName("Error"),
                    SyntaxFactory.IdentifierName("Create")),
                SyntaxFactory.ArgumentList(SyntaxFactory.SingletonSeparatedList(SyntaxFactory.Argument(template))));

            return ReplaceArgument(call, argument, AddDetails(error, details));
        }

        return null;
    }

    private static ExpressionSyntax AddDetails(ExpressionSyntax error, List<(string Name, ExpressionSyntax Value)> details)
    {
        foreach (var (name, value) in details)
        {
            error = SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, error, SyntaxFactory.IdentifierName("WithDetail")),
                SyntaxFactory.ArgumentList(
                    SyntaxFactory.SeparatedList([SyntaxFactory.Argument(LiteralOf(name)), SyntaxFactory.Argument(value.WithoutTrivia())])));
        }

        return error;
    }

    // One value is passed as a name and a value, more than one as details.
    private static IEnumerable<ArgumentSyntax> ContextArguments(ExpressionSyntax template, List<(string Name, ExpressionSyntax Value)> details)
    {
        yield return SyntaxFactory.Argument(template);

        if (details.Count == 1)
        {
            yield return SyntaxFactory.Argument(LiteralOf(details[0].Name));
            yield return SyntaxFactory.Argument(details[0].Value.WithoutTrivia());

            yield break;
        }

        foreach (var (name, value) in details)
        {
            yield return SyntaxFactory.Argument(
                SyntaxFactory.ObjectCreationExpression(
                    SyntaxFactory.IdentifierName("ErrorDetail"),
                    SyntaxFactory.ArgumentList(
                        SyntaxFactory.SeparatedList([SyntaxFactory.Argument(LiteralOf(name)), SyntaxFactory.Argument(value.WithoutTrivia())])),
                    initializer: null));
        }
    }

    private static InvocationExpressionSyntax ReplaceArgument(InvocationExpressionSyntax call, ArgumentSyntax argument, ExpressionSyntax value) =>
        call.ReplaceNode(argument, argument.WithExpression(value));

    private static LiteralExpressionSyntax LiteralOf(string text) =>
        SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal(text));

    // Reads the text with a {name} placeholder for every value. Only a name or a member access has a name that is not made up, and a hole with a format
    // or an alignment would mean something else in a template.
    private static bool TryRead(
        InterpolatedStringExpressionSyntax interpolated,
        out LiteralExpressionSyntax template,
        out List<(string Name, ExpressionSyntax Value)> details)
    {
        template = null!;
        details = [];

        if (interpolated.StringStartToken.Text != "$\"")
        {
            return false;
        }

        var text = new StringBuilder();

        foreach (var content in interpolated.Contents)
        {
            if (content is InterpolatedStringTextSyntax part)
            {
                text.Append(part.TextToken.Text);
            }
            else if (content is InterpolationSyntax { AlignmentClause: null, FormatClause: null } hole
                && NameOf(hole.Expression) is { } name
                && !details.Exists(detail => detail.Name == name))
            {
                details.Add((name, hole.Expression));
                text.Append('{').Append(name).Append('}');
            }
            else
            {
                return false;
            }
        }

        template = SyntaxFactory.LiteralExpression(
            SyntaxKind.StringLiteralExpression,
            SyntaxFactory.Literal("\"" + text.ToString() + "\"", text.ToString()));

        return details.Count > 0;
    }

    private static string? NameOf(ExpressionSyntax expression) => expression switch
    {
        IdentifierNameSyntax identifier => identifier.Identifier.ValueText,
        MemberAccessExpressionSyntax access when IsPlainMemberAccess(access) => ((IdentifierNameSyntax)access.Name).Identifier.ValueText,
        _ => null,
    };

    private static bool IsPlainMemberAccess(MemberAccessExpressionSyntax access) =>
        access.IsKind(SyntaxKind.SimpleMemberAccessExpression)
        && access.Name is IdentifierNameSyntax
        && access.Expression is IdentifierNameSyntax or MemberAccessExpressionSyntax or ThisExpressionSyntax;
}
