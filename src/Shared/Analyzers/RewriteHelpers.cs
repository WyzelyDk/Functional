using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Wyzely.Functional;

/// <summary>
/// What the suggestion code fixes of <c>Result</c> and <c>Maybe</c> both need to rewrite a call: read the lambda that is passed to it,
/// rename the call, and build an argument list. The file is compiled into each code fix project as internal source,
/// so no shared assembly has to ship with a package.
/// </summary>
internal static class RewriteHelpers
{
    /// <summary>Rewrites <c>receiver.Map(x => x)</c> and every other step that does nothing into <c>receiver</c>.</summary>
    /// <param name="invocation">The call of the step.</param>
    /// <returns>The receiver of the call, or <see langword="null"/> when the call is not written as a member access.</returns>
    internal static SyntaxNode? RemoveStep(InvocationExpressionSyntax invocation) =>
        invocation.Expression is MemberAccessExpressionSyntax access ? access.Expression.WithTriviaFrom(invocation) : null;

    /// <summary>
    /// Rewrites <c>receiver.ValueOr(fallback)</c> into <c>receiver.ValueOr(() => fallback)</c>, and keeps the name of the argument if there is one.
    /// </summary>
    /// <param name="invocation">The call of <c>ValueOr</c>.</param>
    /// <returns>The call of <c>ValueOr</c> with a function, or <see langword="null"/>.</returns>
    internal static SyntaxNode? ValueOrFunction(InvocationExpressionSyntax invocation)
    {
        var arguments = invocation.ArgumentList.Arguments;

        if (invocation.Expression is not MemberAccessExpressionSyntax || arguments.Count != 1)
        {
            return null;
        }

        var function = SyntaxFactory.ParenthesizedLambdaExpression(SyntaxFactory.ParameterList(), StripParentheses(arguments[0].Expression));

        return invocation.WithArgumentList(ArgumentsOf([arguments[0].WithExpression(function)]));
    }

    /// <summary>Renames a call, written as a member access or as a plain name.</summary>
    /// <param name="invocation">The call.</param>
    /// <param name="name">The new name.</param>
    /// <returns>The call with the new name and the arguments it had.</returns>
    internal static InvocationExpressionSyntax Renamed(InvocationExpressionSyntax invocation, string name) =>
        invocation.Expression is MemberAccessExpressionSyntax access
            ? invocation.WithExpression(access.WithName(SyntaxFactory.IdentifierName(name)))
            : invocation.WithExpression(SyntaxFactory.IdentifierName(name));

    /// <summary>Reads the first argument of a call when it is a lambda.</summary>
    /// <param name="invocation">The call.</param>
    /// <param name="lambda">The lambda.</param>
    /// <returns><see langword="true"/> when the first argument is a lambda.</returns>
    internal static bool TryGetFirstLambda(InvocationExpressionSyntax invocation, out LambdaExpressionSyntax lambda)
    {
        lambda = null!;

        if (invocation.ArgumentList.Arguments.Count > 0 && invocation.ArgumentList.Arguments[0].Expression is LambdaExpressionSyntax found)
        {
            lambda = found;

            return true;
        }

        return false;
    }

    /// <summary>Gets the expression a lambda returns: its expression body, or the expression of its single return statement.</summary>
    /// <param name="lambda">The lambda.</param>
    /// <returns>The expression, without parentheses, or <see langword="null"/> when the lambda does something else.</returns>
    internal static ExpressionSyntax? ReturnedExpression(LambdaExpressionSyntax lambda) => lambda.Body switch
    {
        ExpressionSyntax expression => StripParentheses(expression),
        BlockSyntax { Statements.Count: 1 } block when block.Statements[0] is ReturnStatementSyntax { Expression: { } returned } => StripParentheses(returned),
        _ => null,
    };

    /// <summary>Gives a lambda another expression as the thing it returns, keeping the block of a single return if it has one.</summary>
    /// <param name="lambda">The lambda.</param>
    /// <param name="expression">The expression to return.</param>
    /// <returns>The lambda, with the expression in place of what it returned.</returns>
    internal static LambdaExpressionSyntax WithReturned(LambdaExpressionSyntax lambda, ExpressionSyntax expression) => lambda.Body switch
    {
        BlockSyntax { Statements.Count: 1 } block when block.Statements[0] is ReturnStatementSyntax returned =>
            lambda.WithBody(block.WithStatements(SyntaxFactory.SingletonList<StatementSyntax>(returned.WithExpression(expression)))),
        _ => lambda.WithBody(expression),
    };

    /// <summary>Strips the parentheses around an expression.</summary>
    /// <param name="expression">The expression.</param>
    /// <returns>The expression inside.</returns>
    internal static ExpressionSyntax StripParentheses(ExpressionSyntax expression)
    {
        while (expression is ParenthesizedExpressionSyntax parenthesized)
        {
            expression = parenthesized.Expression;
        }

        return expression;
    }

    /// <summary>Determines whether a property of a diagnostic is the flag <c>true</c>.</summary>
    /// <param name="properties">The properties of the diagnostic.</param>
    /// <param name="name">The name of the property.</param>
    /// <returns><see langword="true"/> when the analyzer put the flag on the diagnostic.</returns>
    internal static bool IsTrue(ImmutableDictionary<string, string?> properties, string name) =>
        properties.TryGetValue(name, out var value) && value == "true";

    /// <summary>Builds an argument list.</summary>
    /// <param name="arguments">The arguments.</param>
    /// <returns>The list.</returns>
    internal static ArgumentListSyntax ArgumentsOf(IEnumerable<ArgumentSyntax> arguments) =>
        SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(arguments));
}
