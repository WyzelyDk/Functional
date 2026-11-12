using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;

namespace Wyzely.Functional;

/// <summary>
/// What the analyzers of <c>Result</c> and <c>Maybe</c> both need to read an operation.
/// The file is compiled into each analyzer project as internal source, so no shared assembly has to ship with a package.
/// </summary>
internal static partial class OperationHelpers
{
    /// <summary>Strips the implicit conversions around an operation. The operation tree of C# has no node for parentheses.</summary>
    /// <param name="operation">The operation to unwrap.</param>
    /// <returns>The operation inside.</returns>
    internal static IOperation Unwrap(IOperation operation)
    {
        while (operation is IConversionOperation { IsImplicit: true } conversion)
        {
            operation = conversion.Operand;
        }

        return operation;
    }

    /// <summary>
    /// Determines whether evaluating an operation ahead of time costs nothing and cannot change anything: a constant, or a read of a local,
    /// a parameter or a field. Anything else (a call, an allocation) has to stay lazy when a rewrite would otherwise evaluate it on every call.
    /// </summary>
    /// <param name="operation">The operation to test.</param>
    /// <returns><see langword="true"/> for a cheap and safe operation.</returns>
    internal static bool IsCheap(IOperation operation)
    {
        var inner = Unwrap(operation);

        return inner.ConstantValue.HasValue
            || inner is ILocalReferenceOperation or IParameterReferenceOperation or IFieldReferenceOperation or IDefaultValueOperation;
    }

    /// <summary>
    /// Determines whether an expression is a name, or names joined by dots: an expression whose evaluation has no effect,
    /// so that two copies of it read the same value.
    /// </summary>
    /// <param name="node">The expression.</param>
    /// <returns><see langword="true"/> for a plain read.</returns>
    internal static bool IsPlain(SyntaxNode node) => node switch
    {
        IdentifierNameSyntax or ThisExpressionSyntax => true,
        MemberAccessExpressionSyntax access when access.IsKind(SyntaxKind.SimpleMemberAccessExpression)
            => IsPlain(access.Expression),
        _ => false,
    };

    /// <summary>Determines whether two expressions are written the same way, apart from white space and comments.</summary>
    /// <param name="first">The first expression.</param>
    /// <param name="second">The second expression.</param>
    /// <returns><see langword="true"/> when the text is the same.</returns>
    internal static bool SameText(SyntaxNode first, SyntaxNode second) => first.WithoutTrivia().ToString() == second.WithoutTrivia().ToString();
}
