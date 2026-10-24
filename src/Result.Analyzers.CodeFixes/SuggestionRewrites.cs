using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Wyzely.Functional.ResultAnalyzers;

/// <summary>
/// The rewrites behind the fixes of the suggestion rules. Each one takes the node a diagnostic points at, and the properties that the analyzer put on
/// the diagnostic, and returns the node that replaces it, or <see langword="null"/> when the code does not have the expected shape any more.
/// </summary>
internal static class SuggestionRewrites
{
    /// <summary>WYZ0103: <c>Bind(x => cond ? f(x) : Result.Success(x))</c> becomes <c>BindIf(() => cond, x => f(x))</c>.</summary>
    /// <param name="invocation">The call of <c>Bind</c>.</param>
    /// <param name="properties">The properties of the diagnostic.</param>
    /// <returns>The call of <c>BindIf</c>, or <see langword="null"/>.</returns>
    internal static SyntaxNode? BindIf(InvocationExpressionSyntax invocation, ImmutableDictionary<string, string?> properties) =>
        ConditionalRewrite(invocation, properties, "BindIf");

    /// <summary>WYZ0104: <c>Map(x => cond ? f(x) : x)</c> becomes <c>MapIf(() => cond, x => f(x))</c>.</summary>
    /// <param name="invocation">The call of <c>Map</c>.</param>
    /// <param name="properties">The properties of the diagnostic.</param>
    /// <returns>The call of <c>MapIf</c>, or <see langword="null"/>.</returns>
    internal static SyntaxNode? MapIf(InvocationExpressionSyntax invocation, ImmutableDictionary<string, string?> properties) =>
        ConditionalRewrite(invocation, properties, "MapIf");

    /// <summary>WYZ0105: <c>Ensure(x => !cond, error)</c> becomes <c>EnsureNot(x => cond, error)</c>.</summary>
    /// <param name="invocation">The call of <c>Ensure</c>.</param>
    /// <returns>The call of <c>EnsureNot</c>, or <see langword="null"/>.</returns>
    internal static SyntaxNode? EnsureNot(InvocationExpressionSyntax invocation)
    {
        if (!TryGetFirstLambda(invocation, out var lambda)
            || ReturnedExpression(lambda) is not PrefixUnaryExpressionSyntax { RawKind: (int)SyntaxKind.LogicalNotExpression } negation)
        {
            return null;
        }

        var arguments = invocation.ArgumentList.Arguments;
        var condition = WithReturned(lambda, StripParentheses(negation.Operand));

        return Renamed(invocation, "EnsureNot").WithArgumentList(ArgumentsOf(arguments.Replace(arguments[0], arguments[0].WithExpression(condition))));
    }

    /// <summary>WYZ0106: <c>Bind(x => x is null ? Result.Failure&lt;T&gt;(e) : Result.Success(x))</c> becomes <c>EnsureNotNull(e)</c>.</summary>
    /// <param name="invocation">The call of <c>Bind</c>.</param>
    /// <param name="properties">The properties of the diagnostic.</param>
    /// <returns>The call of <c>EnsureNotNull</c>, or <see langword="null"/>.</returns>
    internal static SyntaxNode? EnsureNotNull(InvocationExpressionSyntax invocation, ImmutableDictionary<string, string?> properties)
    {
        if (!TryGetFirstLambda(invocation, out var lambda) || ReturnedExpression(lambda) is not ConditionalExpressionSyntax conditional)
        {
            return null;
        }

        var failure = new[] { conditional.WhenTrue, conditional.WhenFalse }.OfType<InvocationExpressionSyntax>().FirstOrDefault(IsFailureCall);

        if (failure is null || failure.ArgumentList.Arguments.Count != 1)
        {
            return null;
        }

        var error = failure.ArgumentList.Arguments[0].Expression;

        if (IsTrue(properties, "Lazy"))
        {
            // A message cannot be created lazily on its own, so it goes through Error.Create.
            var body = IsTrue(properties, "Message")
                ? SyntaxFactory.InvocationExpression(
                    SyntaxFactory.MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        SyntaxFactory.IdentifierName("Error"),
                        SyntaxFactory.IdentifierName("Create")),
                    ArgumentsOf([SyntaxFactory.Argument(error)]))
                : error;

            error = SyntaxFactory.ParenthesizedLambdaExpression(SyntaxFactory.ParameterList(), body);
        }

        return Renamed(invocation, "EnsureNotNull").WithArgumentList(ArgumentsOf([SyntaxFactory.Argument(error)]));
    }

    /// <summary>WYZ0107: <c>Bind(a => f(a).Map(b => (a, b)))</c> becomes <c>BindZip(a => f(a))</c>.</summary>
    /// <param name="invocation">The call of <c>Bind</c>.</param>
    /// <returns>The call of <c>BindZip</c>, or <see langword="null"/>.</returns>
    internal static SyntaxNode? BindZip(InvocationExpressionSyntax invocation)
    {
        if (!TryGetFirstLambda(invocation, out var lambda)
            || ReturnedExpression(lambda) is not InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax map })
        {
            return null;
        }

        var zipped = WithReturned(lambda, map.Expression);
        var arguments = invocation.ArgumentList.Arguments;

        return Renamed(invocation, "BindZip").WithArgumentList(ArgumentsOf(arguments.Replace(arguments[0], arguments[0].WithExpression(zipped))));
    }

    /// <summary>WYZ0108: <c>Result.Failure&lt;T&gt;(r.GetError())</c> becomes <c>r.ConvertFailure&lt;T&gt;()</c>.</summary>
    /// <param name="invocation">The call of <c>Failure</c>.</param>
    /// <returns>The call of <c>ConvertFailure</c>, or <see langword="null"/>.</returns>
    internal static SyntaxNode? ConvertFailure(InvocationExpressionSyntax invocation)
    {
        if (invocation.Expression is not MemberAccessExpressionSyntax { Name: GenericNameSyntax generic }
            || invocation.ArgumentList.Arguments.Count != 1
            || StripParentheses(invocation.ArgumentList.Arguments[0].Expression)
                is not InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax getError })
        {
            return null;
        }

        var name = SyntaxFactory.GenericName(SyntaxFactory.Identifier("ConvertFailure"), generic.TypeArgumentList);

        return SyntaxFactory.InvocationExpression(getError.WithName(name)).WithTriviaFrom(invocation);
    }

    /// <summary>
    /// WYZ0109: <c>ok ? Result.Success() : Result.Failure(e)</c> becomes <c>Result.SuccessIf(ok, e)</c>, and the reverse becomes <c>FailureIf</c>.
    /// </summary>
    /// <param name="conditional">The conditional expression.</param>
    /// <param name="properties">The properties of the diagnostic.</param>
    /// <returns>The call of <c>SuccessIf</c> or <c>FailureIf</c>, or <see langword="null"/>.</returns>
    internal static SyntaxNode? SuccessIf(ConditionalExpressionSyntax conditional, ImmutableDictionary<string, string?> properties)
    {
        var failure = new[] { conditional.WhenTrue, conditional.WhenFalse }.OfType<InvocationExpressionSyntax>().FirstOrDefault(IsFailureCall);

        if (failure is null || failure.ArgumentList.Arguments.Count != 1 || !properties.TryGetValue("Method", out var method) || method is null)
        {
            return null;
        }

        var call = SyntaxFactory.InvocationExpression(
            SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                SyntaxFactory.IdentifierName("Result"),
                SyntaxFactory.IdentifierName(method)),
            ArgumentsOf([SyntaxFactory.Argument(StripParentheses(conditional.Condition)), failure.ArgumentList.Arguments[0]]));

        return call.WithTriviaFrom(conditional);
    }

    /// <summary>WYZ0110: <c>Bind(x => Result.Try(() => f(x), h))</c> becomes <c>MapTry(x => f(x), h)</c>.</summary>
    /// <param name="invocation">The call of <c>Bind</c>.</param>
    /// <returns>The call of <c>MapTry</c>, or <see langword="null"/>.</returns>
    internal static SyntaxNode? MapTry(InvocationExpressionSyntax invocation)
    {
        if (!TryGetFirstLambda(invocation, out var lambda)
            || ReturnedExpression(lambda) is not InvocationExpressionSyntax attempt
            || attempt.ArgumentList.Arguments.Count == 0
            || attempt.ArgumentList.Arguments[0].Expression is not ParenthesizedLambdaExpressionSyntax function)
        {
            return null;
        }

        var mapped = lambda.WithBody(function.Body).WithAsyncKeyword(function.AsyncKeyword);
        var rest = attempt.ArgumentList.Arguments.Skip(1);
        var arguments = new[] { SyntaxFactory.Argument(mapped) }.Concat(rest);

        return Renamed(invocation, "MapTry").WithArgumentList(ArgumentsOf(arguments));
    }

    // BindIf and MapIf: the lambda that decides, and the lambda that does the work, in place of the one conditional lambda.
    private static InvocationExpressionSyntax? ConditionalRewrite(
        InvocationExpressionSyntax invocation,
        ImmutableDictionary<string, string?> properties,
        string name)
    {
        if (!TryGetFirstLambda(invocation, out var lambda) || ReturnedExpression(lambda) is not ConditionalExpressionSyntax conditional)
        {
            return null;
        }

        var negate = IsTrue(properties, "Negate");
        var work = negate ? conditional.WhenFalse : conditional.WhenTrue;
        var condition = negate ? Negated(conditional.Condition) : StripParentheses(conditional.Condition);
        var decide = IsTrue(properties, "UsesParameter")
            ? WithReturned(lambda, condition)
            : SyntaxFactory.ParenthesizedLambdaExpression(SyntaxFactory.ParameterList(), condition);

        return Renamed(invocation, name).WithArgumentList(ArgumentsOf(
            [SyntaxFactory.Argument(decide), SyntaxFactory.Argument(WithReturned(lambda, StripParentheses(work)))]));
    }

    private static InvocationExpressionSyntax Renamed(InvocationExpressionSyntax invocation, string name) =>
        invocation.Expression is MemberAccessExpressionSyntax access
            ? invocation.WithExpression(access.WithName(SyntaxFactory.IdentifierName(name)))
            : invocation.WithExpression(SyntaxFactory.IdentifierName(name));

    private static bool TryGetFirstLambda(InvocationExpressionSyntax invocation, out LambdaExpressionSyntax lambda)
    {
        lambda = null!;

        if (invocation.ArgumentList.Arguments.Count > 0 && invocation.ArgumentList.Arguments[0].Expression is LambdaExpressionSyntax found)
        {
            lambda = found;

            return true;
        }

        return false;
    }

    // The expression a lambda returns: its expression body, or the expression of its single return statement.
    private static ExpressionSyntax? ReturnedExpression(LambdaExpressionSyntax lambda) => lambda.Body switch
    {
        ExpressionSyntax expression => StripParentheses(expression),
        BlockSyntax { Statements.Count: 1 } block when block.Statements[0] is ReturnStatementSyntax { Expression: { } returned } => StripParentheses(returned),
        _ => null,
    };

    // The same lambda, with a different expression as the thing it returns.
    private static LambdaExpressionSyntax WithReturned(LambdaExpressionSyntax lambda, ExpressionSyntax expression) => lambda.Body switch
    {
        BlockSyntax { Statements.Count: 1 } block when block.Statements[0] is ReturnStatementSyntax returned =>
            lambda.WithBody(block.WithStatements(SyntaxFactory.SingletonList<StatementSyntax>(returned.WithExpression(expression)))),
        _ => lambda.WithBody(expression),
    };

    private static ExpressionSyntax StripParentheses(ExpressionSyntax expression)
    {
        while (expression is ParenthesizedExpressionSyntax parenthesized)
        {
            expression = parenthesized.Expression;
        }

        return expression;
    }

    private static PrefixUnaryExpressionSyntax Negated(ExpressionSyntax condition)
    {
        var operand = StripParentheses(condition);
        var simple = operand is IdentifierNameSyntax or InvocationExpressionSyntax or MemberAccessExpressionSyntax or LiteralExpressionSyntax;

        return SyntaxFactory.PrefixUnaryExpression(
            SyntaxKind.LogicalNotExpression,
            simple ? operand : SyntaxFactory.ParenthesizedExpression(operand));
    }

    private static bool IsFailureCall(InvocationExpressionSyntax call) => call.Expression is MemberAccessExpressionSyntax
    {
        Name: IdentifierNameSyntax { Identifier.ValueText: "Failure" } or GenericNameSyntax { Identifier.ValueText: "Failure" },
    };

    private static bool IsTrue(ImmutableDictionary<string, string?> properties, string name) =>
        properties.TryGetValue(name, out var value) && value == "true";

    private static ArgumentListSyntax ArgumentsOf(IEnumerable<ArgumentSyntax> arguments) =>
        SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(arguments));
}
