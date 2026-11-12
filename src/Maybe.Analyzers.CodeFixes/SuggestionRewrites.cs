using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Wyzely.Functional.RewriteHelpers;

namespace Wyzely.Functional.MaybeAnalyzers;

/// <summary>
/// The rewrites behind the fixes of the suggestion rules. Each one takes the node a diagnostic points at, and the properties that the analyzer put on
/// the diagnostic, and returns the node that replaces it, or <see langword="null"/> when the code does not have the expected shape any more.
/// </summary>
internal static class SuggestionRewrites
{
    // The name of the property that the analyzer puts on a diagnostic; the code fix project does not reference the analyzer project.
    private const string LazyProperty = "Lazy";

    /// <summary>WYZ0202: <c>maybe.Filter(p).IsSome</c> becomes <c>maybe.Any(p)</c>.</summary>
    /// <param name="access">The member access that ends in <c>IsSome</c>.</param>
    /// <returns>The call of <c>Any</c>, or <see langword="null"/>.</returns>
    internal static SyntaxNode? Any(MemberAccessExpressionSyntax access) =>
        access.Expression is InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax filter } call
            ? call.WithExpression(filter.WithName(SyntaxFactory.IdentifierName("Any")))
            : null;

    /// <summary>
    /// WYZ0203: <c>m.IsSome ? m.GetValue() : fallback</c> and <c>m.IsNone ? fallback : m.GetValue()</c> become <c>m.ValueOr(fallback)</c>,
    /// and <c>m.ValueOr(() => fallback)</c> when the fallback has to stay lazy.
    /// </summary>
    /// <param name="conditional">The conditional expression.</param>
    /// <param name="properties">The properties of the diagnostic.</param>
    /// <returns>The call of <c>ValueOr</c>, or <see langword="null"/>.</returns>
    internal static SyntaxNode? ConditionalToValueOr(ConditionalExpressionSyntax conditional, ImmutableDictionary<string, string?> properties)
    {
        if (StripParentheses(conditional.Condition) is not MemberAccessExpressionSyntax { Name.Identifier.ValueText: "IsSome" or "IsNone" } check)
        {
            return null;
        }

        var fallback = StripParentheses(check.Name.Identifier.ValueText == "IsSome" ? conditional.WhenFalse : conditional.WhenTrue).WithoutTrivia();
        var valueOr = SyntaxFactory.MemberAccessExpression(
            SyntaxKind.SimpleMemberAccessExpression,
            check.Expression.WithoutTrivia(),
            SyntaxFactory.IdentifierName("ValueOr"));

        return SyntaxFactory.InvocationExpression(valueOr, ArgumentsOf([SyntaxFactory.Argument(FallbackArgument(fallback, properties))]));
    }

    /// <summary>WYZ0204: <c>x is null ? Maybe.None&lt;T&gt;() : Maybe.Some(x)</c> becomes <c>Maybe.From(x)</c>.</summary>
    /// <param name="conditional">The conditional expression.</param>
    /// <returns>The call of <c>From</c>, or <see langword="null"/>.</returns>
    internal static SyntaxNode? From(ConditionalExpressionSyntax conditional)
    {
        // The qualifier is taken from the call of Some, so that it is written the way the code already writes it.
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
            ArgumentsOf([SyntaxFactory.Argument(value.WithoutTrivia())]));
    }

    /// <summary>
    /// WYZ0205: <c>m.Match(v => v, () => fallback)</c> becomes <c>m.ValueOr(fallback)</c>,
    /// and when the fallback has to stay lazy, <c>m.Match(v => v, onNone)</c> becomes <c>m.ValueOr(onNone)</c>.
    /// </summary>
    /// <param name="invocation">The call of <c>Match</c>.</param>
    /// <param name="properties">The properties of the diagnostic.</param>
    /// <returns>The call of <c>ValueOr</c>, or <see langword="null"/>.</returns>
    internal static SyntaxNode? MatchToValueOr(InvocationExpressionSyntax invocation, ImmutableDictionary<string, string?> properties)
    {
        var arguments = invocation.ArgumentList.Arguments;

        if (invocation.Expression is not MemberAccessExpressionSyntax
            || arguments.Count != 2
            || OnNone(arguments)?.Expression is not LambdaExpressionSyntax none)
        {
            return null;
        }

        var fallback = IsTrue(properties, LazyProperty) ? none.WithoutTrivia() : ReturnedExpression(none)?.WithoutTrivia();

        return fallback is null ? null : Renamed(invocation, "ValueOr").WithArgumentList(ArgumentsOf([SyntaxFactory.Argument(fallback)]));
    }

    /// <summary>WYZ0206: <c>m.Bind(x => Maybe.Some(f(x)))</c> becomes <c>m.Map(x => f(x))</c>.</summary>
    /// <param name="invocation">The call of <c>Bind</c>.</param>
    /// <returns>The call of <c>Map</c>, or <see langword="null"/>.</returns>
    internal static SyntaxNode? BindToMap(InvocationExpressionSyntax invocation)
    {
        if (!TryGetFirstLambda(invocation, out var lambda)
            || ReturnedExpression(lambda) is not InvocationExpressionSyntax { ArgumentList.Arguments.Count: 1 } some
            || !IsCallOf(some, "Some"))
        {
            return null;
        }

        var value = some.ArgumentList.Arguments[0].Expression.WithTriviaFrom(some);

        return Renamed(invocation, "Map").WithArgumentList(ArgumentsOf([SyntaxFactory.Argument(WithReturned(lambda, value))]));
    }

    // The fallback as the argument of ValueOr: itself, or a function without a parameter.
    private static ExpressionSyntax FallbackArgument(ExpressionSyntax fallback, ImmutableDictionary<string, string?> properties) =>
        IsTrue(properties, LazyProperty)
            ? SyntaxFactory.ParenthesizedLambdaExpression(SyntaxFactory.ParameterList(), fallback)
            : fallback;

    // The argument that is the function for none: the one named onNone, or the second one when it is not named.
    private static ArgumentSyntax? OnNone(SeparatedSyntaxList<ArgumentSyntax> arguments)
    {
        var named = arguments.FirstOrDefault(argument => argument.NameColon?.Name.Identifier.ValueText == "onNone");

        return named ?? (arguments[1].NameColon is null ? arguments[1] : null);
    }

    // A call of a method with the given name: Maybe.Some(x), Maybe.Some<int>(x) and Some(x).
    private static bool IsCallOf(InvocationExpressionSyntax call, string name) =>
        (call.Expression is MemberAccessExpressionSyntax access ? access.Name : call.Expression as SimpleNameSyntax)?.Identifier.ValueText == name;

    // The expression that a null check is about: x in x is null, x is not null, x == null and x != null.
    private static ExpressionSyntax? NullCheckedExpression(ExpressionSyntax condition) => condition switch
    {
        IsPatternExpressionSyntax pattern => pattern.Expression,
        BinaryExpressionSyntax binary when IsNull(binary.Right) => binary.Left,
        BinaryExpressionSyntax binary when IsNull(binary.Left) => binary.Right,
        _ => null,
    };

    private static bool IsNull(ExpressionSyntax expression) => StripParentheses(expression).IsKind(SyntaxKind.NullLiteralExpression);
}
