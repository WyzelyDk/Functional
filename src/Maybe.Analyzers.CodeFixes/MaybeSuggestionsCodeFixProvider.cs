using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Wyzely.Functional.MaybeAnalyzers;

/// <summary>Rewrites the code that the suggestion rules (WYZ0202 to WYZ0204) point at into the method of the library that says what it does.</summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(MaybeSuggestionsCodeFixProvider))]
[System.Composition.Shared]
public sealed class MaybeSuggestionsCodeFixProvider : RewriteCodeFixProvider
{
    /// <inheritdoc />
    protected override ImmutableDictionary<string, string> Titles { get; } = new Dictionary<string, string>
    {
        ["WYZ0202"] = "Use Any",
        ["WYZ0203"] = "Use ValueOr",
        ["WYZ0204"] = "Use Maybe.From",
    }.ToImmutableDictionary();

    /// <inheritdoc />
    // The member access that ends in IsSome for WYZ0202, otherwise the conditional expression.
    protected override bool IsTarget(SyntaxNode node, string id) =>
        id == "WYZ0202" ? node is MemberAccessExpressionSyntax : node is ConditionalExpressionSyntax;

    /// <inheritdoc />
    protected override SyntaxNode? Rewrite(SyntaxNode target, Diagnostic diagnostic) => (target, diagnostic.Id) switch
    {
        (MemberAccessExpressionSyntax access, "WYZ0202") => Any(access),
        (ConditionalExpressionSyntax conditional, "WYZ0203") => ValueOr(conditional),
        (ConditionalExpressionSyntax conditional, "WYZ0204") => From(conditional),
        _ => null,
    };

    // WYZ0202: maybe.Filter(p).IsSome becomes maybe.Any(p).
    private static InvocationExpressionSyntax? Any(MemberAccessExpressionSyntax access) =>
        access.Expression is InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax filter } call
            ? call.WithExpression(filter.WithName(SyntaxFactory.IdentifierName("Any")))
            : null;

    // WYZ0203: maybe.IsSome ? maybe.GetValue() : fallback becomes maybe.ValueOr(fallback), and maybe.IsNone ? fallback : maybe.GetValue() too.
    private static InvocationExpressionSyntax? ValueOr(ConditionalExpressionSyntax conditional)
    {
        if (StripParentheses(conditional.Condition) is not MemberAccessExpressionSyntax { Name.Identifier.ValueText: "IsSome" or "IsNone" } check)
        {
            return null;
        }

        var fallback = check.Name.Identifier.ValueText == "IsSome" ? conditional.WhenFalse : conditional.WhenTrue;

        return SyntaxFactory.InvocationExpression(
            SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                check.Expression.WithoutTrivia(),
                SyntaxFactory.IdentifierName("ValueOr")),
            SyntaxFactory.ArgumentList(SyntaxFactory.SingletonSeparatedList(SyntaxFactory.Argument(StripParentheses(fallback).WithoutTrivia()))));
    }

    // WYZ0204: x is null ? Maybe.None<T>() : Maybe.Some(x) becomes Maybe.From(x). The qualifier is taken from the call of Some, so that it is written
    // the way the code already writes it.
    private static InvocationExpressionSyntax? From(ConditionalExpressionSyntax conditional)
    {
        var some = new[] { conditional.WhenTrue, conditional.WhenFalse }
            .Select(StripParentheses)
            .OfType<InvocationExpressionSyntax>()
            .FirstOrDefault(call => call.Expression is MemberAccessExpressionSyntax { Name.Identifier.ValueText: "Some" });

        var value = NullCheckedExpression(StripParentheses(conditional.Condition));

        if (some is null || value is null)
        {
            return null;
        }

        var access = (MemberAccessExpressionSyntax)some.Expression;

        return SyntaxFactory.InvocationExpression(
            access.WithName(SyntaxFactory.IdentifierName("From")),
            SyntaxFactory.ArgumentList(SyntaxFactory.SingletonSeparatedList(SyntaxFactory.Argument(value.WithoutTrivia()))));
    }

    // The expression that a null check is about: x in x is null, x is not null, x == null and x != null.
    private static ExpressionSyntax? NullCheckedExpression(ExpressionSyntax condition) => condition switch
    {
        IsPatternExpressionSyntax pattern => pattern.Expression,
        BinaryExpressionSyntax binary when IsNull(binary.Right) => binary.Left,
        BinaryExpressionSyntax binary when IsNull(binary.Left) => binary.Right,
        _ => null,
    };

    private static bool IsNull(ExpressionSyntax expression) => StripParentheses(expression).IsKind(SyntaxKind.NullLiteralExpression);

    private static ExpressionSyntax StripParentheses(ExpressionSyntax expression)
    {
        while (expression is ParenthesizedExpressionSyntax parenthesized)
        {
            expression = parenthesized.Expression;
        }

        return expression;
    }
}
