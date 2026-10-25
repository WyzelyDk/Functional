using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Wyzely.Functional.MaybeAnalyzers;

/// <summary>
/// Decides whether a call of <c>GetValue()</c> is protected by a check of the same maybe: an enclosing <c>if</c>, conditional
/// expression, <c>&amp;&amp;</c> or <c>||</c>, or an earlier <c>if</c> in the same block that leaves it.
/// </summary>
internal static class AccessGuards
{
    /// <summary>Determines whether the call is known to run only when the receiver has a value.</summary>
    /// <param name="call">The call of <c>GetValue()</c>.</param>
    /// <returns><see langword="true"/> when a check of the same receiver guarantees that it has a value.</returns>
    internal static bool IsGuarded(InvocationExpressionSyntax call)
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

            if (GuardedByEnclosing(parent, child, receiver) || GuardedByEarlierExit(parent, child, receiver))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsBoundary(SyntaxNode node) =>
        node is BaseMethodDeclarationSyntax or AccessorDeclarationSyntax or LocalFunctionStatementSyntax;

    private static bool GuardedByEnclosing(SyntaxNode parent, SyntaxNode child, string receiver) => parent switch
    {
        IfStatementSyntax ifStatement when child == ifStatement.Statement
            => ProvesValue(ifStatement.Condition, receiver, whenTrue: true),
        ElseClauseSyntax { Parent: IfStatementSyntax ifStatement }
            => ProvesValue(ifStatement.Condition, receiver, whenTrue: false),
        ConditionalExpressionSyntax conditional => GuardedByConditional(conditional, child, receiver),
        BinaryExpressionSyntax binary => GuardedByLogicalOperator(binary, child, receiver),
        _ => false,
    };

    private static bool GuardedByConditional(ConditionalExpressionSyntax conditional, SyntaxNode child, string receiver)
    {
        if (child == conditional.WhenTrue)
        {
            return ProvesValue(conditional.Condition, receiver, whenTrue: true);
        }

        return child == conditional.WhenFalse && ProvesValue(conditional.Condition, receiver, whenTrue: false);
    }

    // The right operand of `&&` runs only when the left one is true, and that of `||` only when it is false.
    private static bool GuardedByLogicalOperator(BinaryExpressionSyntax binary, SyntaxNode child, string receiver)
    {
        if (child != binary.Right)
        {
            return false;
        }

        if (binary.IsKind(SyntaxKind.LogicalAndExpression))
        {
            return ProvesValue(binary.Left, receiver, whenTrue: true);
        }

        return binary.IsKind(SyntaxKind.LogicalOrExpression) && ProvesValue(binary.Left, receiver, whenTrue: false);
    }

    // `if (check) { return; }` followed by the call: when execution gets here, the condition was false.
    private static bool GuardedByEarlierExit(SyntaxNode parent, SyntaxNode child, string receiver) =>
        parent is BlockSyntax block
        && child is StatementSyntax statement
        && block.Statements
            .TakeWhile(earlier => earlier != statement)
            .Any(earlier => earlier is IfStatementSyntax { Else: null } ifStatement
                && Exits(ifStatement.Statement)
                && ProvesValue(ifStatement.Condition, receiver, whenTrue: false));

    private static bool Exits(StatementSyntax statement) => statement switch
    {
        ReturnStatementSyntax or ThrowStatementSyntax or ContinueStatementSyntax or BreakStatementSyntax => true,
        BlockSyntax block => block.Statements.Count > 0 && Exits(block.Statements[block.Statements.Count - 1]),
        _ => false,
    };

    // Does the expression, when it evaluates to `whenTrue`, prove that the receiver has a value?
    private static bool ProvesValue(ExpressionSyntax expression, string receiver, bool whenTrue) => expression switch
    {
        ParenthesizedExpressionSyntax parenthesized
            => ProvesValue(parenthesized.Expression, receiver, whenTrue),
        PrefixUnaryExpressionSyntax negation when negation.IsKind(SyntaxKind.LogicalNotExpression)
            => ProvesValue(negation.Operand, receiver, !whenTrue),
        BinaryExpressionSyntax binary
            => ProvesByLogicalOperator(binary, receiver, whenTrue),
        MemberAccessExpressionSyntax member
            => Proves(member.Expression, member.Name.Identifier.Text, receiver, whenTrue),
        InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax member }
            => Proves(member.Expression, member.Name.Identifier.Text, receiver, whenTrue),
        _ => false,
    };

    // `a && b` proves a value when it is true and either operand proves it; `a || b` when it is false and either operand proves it.
    private static bool ProvesByLogicalOperator(BinaryExpressionSyntax binary, string receiver, bool whenTrue)
    {
        if (binary.IsKind(SyntaxKind.LogicalAndExpression))
        {
            return whenTrue && (ProvesValue(binary.Left, receiver, true) || ProvesValue(binary.Right, receiver, true));
        }

        if (binary.IsKind(SyntaxKind.LogicalOrExpression))
        {
            return !whenTrue && (ProvesValue(binary.Left, receiver, false) || ProvesValue(binary.Right, receiver, false));
        }

        return false;
    }

    private static bool Proves(ExpressionSyntax subject, string name, string receiver, bool whenTrue)
    {
        if (subject.WithoutTrivia().ToString() != receiver)
        {
            return false;
        }

        // What it means for the check to be true: IsSome and TryGetValue say "has a value", IsNone says "has none".
        bool? valueWhenTrue = name switch
        {
            "IsSome" or "TryGetValue" => true,
            "IsNone" => false,
            _ => null,
        };

        return valueWhenTrue is { } meaning && (whenTrue ? meaning : !meaning);
    }
}
