using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Wyzely.Functional.ResultAnalyzers;

/// <summary>
/// Decides whether a call of <c>GetError()</c> or <c>GetValue()</c> is protected by a check of the same result: an enclosing
/// <c>if</c>, conditional expression, <c>&amp;&amp;</c> or <c>||</c>, or an earlier <c>if</c> in the same block that leaves it.
/// </summary>
internal static class AccessGuards
{
    /// <summary>Determines whether the call is known to run only when the receiver is in the state the call needs.</summary>
    /// <param name="call">The call of <c>GetError()</c> or <c>GetValue()</c>.</param>
    /// <param name="needsFailure">
    /// <see langword="true"/> for <c>GetError()</c>, which needs a failed result; <see langword="false"/> for <c>GetValue()</c>.
    /// </param>
    /// <returns><see langword="true"/> when a check of the same receiver guarantees the state.</returns>
    internal static bool IsGuarded(InvocationExpressionSyntax call, bool needsFailure)
    {
        if (call.Expression is not MemberAccessExpressionSyntax access)
        {
            return true;
        }

        // A receiver that is itself a call is evaluated again by any check, so no check can be about the same value.
        if (access.Expression.DescendantNodesAndSelf().OfType<InvocationExpressionSyntax>().Any())
        {
            return false;
        }

        var receiver = access.Expression.WithoutTrivia().ToString();
        SyntaxNode child = call;

        for (var parent = call.Parent; parent is not null; child = parent, parent = parent.Parent)
        {
            if (IsBoundary(parent))
            {
                return false;
            }

            if (GuardedByEnclosing(parent, child, receiver, needsFailure) || GuardedByEarlierExit(parent, child, receiver, needsFailure))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsBoundary(SyntaxNode node) =>
        node is BaseMethodDeclarationSyntax or AccessorDeclarationSyntax or LocalFunctionStatementSyntax;

    private static bool GuardedByEnclosing(SyntaxNode parent, SyntaxNode child, string receiver, bool needsFailure) => parent switch
    {
        IfStatementSyntax ifStatement when child == ifStatement.Statement
            => Implies(ifStatement.Condition, receiver, needsFailure, whenTrue: true),
        ElseClauseSyntax { Parent: IfStatementSyntax ifStatement }
            => Implies(ifStatement.Condition, receiver, needsFailure, whenTrue: false),
        ConditionalExpressionSyntax conditional when child == conditional.WhenTrue
            => Implies(conditional.Condition, receiver, needsFailure, whenTrue: true),
        ConditionalExpressionSyntax conditional when child == conditional.WhenFalse
            => Implies(conditional.Condition, receiver, needsFailure, whenTrue: false),
        BinaryExpressionSyntax binary when binary.IsKind(SyntaxKind.LogicalAndExpression) && child == binary.Right
            => Implies(binary.Left, receiver, needsFailure, whenTrue: true),
        BinaryExpressionSyntax binary when binary.IsKind(SyntaxKind.LogicalOrExpression) && child == binary.Right
            => Implies(binary.Left, receiver, needsFailure, whenTrue: false),
        _ => false,
    };

    // `if (check) { return; }` followed by the call: when execution gets here, the condition was false.
    private static bool GuardedByEarlierExit(SyntaxNode parent, SyntaxNode child, string receiver, bool needsFailure) =>
        parent is BlockSyntax block
        && child is StatementSyntax statement
        && block.Statements
            .TakeWhile(earlier => earlier != statement)
            .Any(earlier => earlier is IfStatementSyntax { Else: null } ifStatement
                && Exits(ifStatement.Statement)
                && Implies(ifStatement.Condition, receiver, needsFailure, whenTrue: false));

    private static bool Exits(StatementSyntax statement) => statement switch
    {
        ReturnStatementSyntax or ThrowStatementSyntax or ContinueStatementSyntax or BreakStatementSyntax => true,
        BlockSyntax block => block.Statements.Count > 0 && Exits(block.Statements[block.Statements.Count - 1]),
        _ => false,
    };

    // Does the expression, when it evaluates to `whenTrue`, prove that the receiver is failed (needsFailure) or succeeded?
    private static bool Implies(ExpressionSyntax expression, string receiver, bool needsFailure, bool whenTrue) => expression switch
    {
        ParenthesizedExpressionSyntax parenthesized
            => Implies(parenthesized.Expression, receiver, needsFailure, whenTrue),
        PrefixUnaryExpressionSyntax negation when negation.IsKind(SyntaxKind.LogicalNotExpression)
            => Implies(negation.Operand, receiver, needsFailure, !whenTrue),
        BinaryExpressionSyntax conjunction when conjunction.IsKind(SyntaxKind.LogicalAndExpression)
            => whenTrue && (Implies(conjunction.Left, receiver, needsFailure, true) || Implies(conjunction.Right, receiver, needsFailure, true)),
        BinaryExpressionSyntax disjunction when disjunction.IsKind(SyntaxKind.LogicalOrExpression)
            => !whenTrue && (Implies(disjunction.Left, receiver, needsFailure, false) || Implies(disjunction.Right, receiver, needsFailure, false)),
        MemberAccessExpressionSyntax member
            => Proves(member.Expression, member.Name.Identifier.Text, receiver, needsFailure, whenTrue),
        InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax member }
            => Proves(member.Expression, member.Name.Identifier.Text, receiver, needsFailure, whenTrue),
        _ => false,
    };

    private static bool Proves(ExpressionSyntax subject, string name, string receiver, bool needsFailure, bool whenTrue)
    {
        if (subject.WithoutTrivia().ToString() != receiver)
        {
            return false;
        }

        // What it means for the check to be true: IsFailure and TryGetError say "failed", IsSuccess and TryGetValue say "succeeded".
        bool? failedWhenTrue = name switch
        {
            "IsFailure" or "TryGetError" => true,
            "IsSuccess" or "TryGetValue" => false,
            _ => null,
        };

        if (failedWhenTrue is not { } meaning)
        {
            return false;
        }

        var impliedFailure = whenTrue ? meaning : !meaning;

        return impliedFailure == needsFailure;
    }
}
