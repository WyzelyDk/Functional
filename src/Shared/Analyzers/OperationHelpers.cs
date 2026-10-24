using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;

namespace Wyzely.Functional;

/// <summary>
/// What the analyzers of <c>Result</c> and <c>Maybe</c> both need to read an operation. The file is compiled into each analyzer project as internal
/// source, so no shared assembly has to ship with a package.
/// </summary>
internal static class OperationHelpers
{
    /// <summary>Strips the implicit conversions and the parentheses around an operation.</summary>
    /// <param name="operation">The operation to unwrap.</param>
    /// <returns>The operation inside.</returns>
    internal static IOperation Unwrap(IOperation operation)
    {
        while (true)
        {
            switch (operation)
            {
                case IConversionOperation { IsImplicit: true } conversion:
                    operation = conversion.Operand;
                    break;
                case IParenthesizedOperation parenthesized:
                    operation = parenthesized.Operand;
                    break;
                default:
                    return operation;
            }
        }
    }

    /// <summary>
    /// Determines whether evaluating an operation ahead of time costs nothing and cannot change anything: a constant, or a read of a local, a
    /// parameter or a field. Anything else (a call, an allocation) has to stay lazy when a rewrite would otherwise evaluate it on every call.
    /// </summary>
    /// <param name="operation">The operation to test.</param>
    /// <returns><see langword="true"/> for a cheap and safe operation.</returns>
    internal static bool IsCheap(IOperation operation)
    {
        var inner = Unwrap(operation);

        return inner.ConstantValue.HasValue
            || inner is ILocalReferenceOperation or IParameterReferenceOperation or IFieldReferenceOperation or IDefaultValueOperation;
    }
}
