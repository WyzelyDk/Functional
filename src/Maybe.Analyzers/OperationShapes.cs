using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;

namespace Wyzely.Functional.MaybeAnalyzers;

/// <summary>Reads the shapes of expressions that the suggestion rules look for, so that each rule only says what it wants to see.</summary>
internal static class OperationShapes
{
    /// <summary>Determines whether a method is a member of the library's <c>MaybeExtensions</c>.</summary>
    /// <param name="method">The method to test.</param>
    /// <returns><see langword="true"/> for an extension method of the library.</returns>
    internal static bool IsMaybeExtension(IMethodSymbol method) =>
        method.ContainingType.Name == "MaybeExtensions" && method.ContainingNamespace.ToDisplayString() == "Wyzely.Functional";

    /// <summary>
    /// Reads a condition that compares an expression with <see langword="null"/>: <c>is null</c>, <c>is not null</c>, <c>==</c> or <c>!=</c>.
    /// </summary>
    /// <param name="condition">The condition.</param>
    /// <param name="checkedOperation">The expression that is compared with <see langword="null"/>.</param>
    /// <param name="isNullWhenTrue"><see langword="true"/> when the condition holds for <see langword="null"/>.</param>
    /// <returns><see langword="true"/> when the condition is such a comparison.</returns>
    internal static bool TryReadNullCheck(IOperation condition, out IOperation checkedOperation, out bool isNullWhenTrue)
    {
        checkedOperation = null!;
        isNullWhenTrue = false;

        switch (OperationHelpers.Unwrap(condition))
        {
            case IIsPatternOperation pattern when TryReadNullPattern(pattern.Pattern, out isNullWhenTrue):
                checkedOperation = OperationHelpers.Unwrap(pattern.Value);

                return true;
            case IBinaryOperation binary:
                return TryReadNullComparison(binary, out checkedOperation, out isNullWhenTrue);
            default:
                return false;
        }
    }

    /// <summary>Determines whether an operation is none: <c>Maybe.None&lt;T&gt;()</c> or <c>default</c> of a maybe.</summary>
    /// <param name="operation">The operation, without conversions.</param>
    /// <param name="types">The symbols of the compilation.</param>
    /// <returns><see langword="true"/> for none.</returns>
    internal static bool IsNone(IOperation operation, MaybeTypes types) => operation switch
    {
        IInvocationOperation { Arguments.Length: 0 } invocation => IsMaybeFactory(invocation.TargetMethod, "None"),
        IDefaultValueOperation defaulted => types.IsMaybe(defaulted.Type),
        _ => false,
    };

    /// <summary>
    /// Determines whether an operation is <c>Maybe.Some</c> of the value that was compared with <see langword="null"/>: the expression itself for a reference,
    /// or its <c>Value</c> for a nullable value type.
    /// </summary>
    /// <param name="operation">The operation, without conversions.</param>
    /// <param name="checkedOperation">The expression that was compared with <see langword="null"/>.</param>
    /// <returns><see langword="true"/> when <c>Maybe.From</c> of the expression would be the same maybe.</returns>
    internal static bool IsSomeOf(IOperation operation, IOperation checkedOperation)
    {
        if (!TryReadSomeArgument(operation, out var argument))
        {
            return false;
        }

        if (checkedOperation.Type is { IsReferenceType: true })
        {
            return OperationHelpers.SameText(argument.Syntax, checkedOperation.Syntax);
        }

        return IsValueOfNullable(argument, checkedOperation);
    }

    // `null` or `not null` as a pattern.
    private static bool TryReadNullPattern(IPatternOperation pattern, out bool isNullWhenTrue)
    {
        isNullWhenTrue = pattern is IConstantPatternOperation;

        var constant = pattern switch
        {
            IConstantPatternOperation direct => direct,
            INegatedPatternOperation { Pattern: IConstantPatternOperation negated } => negated,
            _ => null,
        };

        return constant is not null && IsNullConstant(constant.Value);
    }

    // `x == null`, `x != null`, `null == x` and `null != x`.
    private static bool TryReadNullComparison(IBinaryOperation binary, out IOperation checkedOperation, out bool isNullWhenTrue)
    {
        checkedOperation = null!;
        isNullWhenTrue = false;

        if (binary.OperatorKind is not (BinaryOperatorKind.Equals or BinaryOperatorKind.NotEquals))
        {
            return false;
        }

        var leftIsNull = IsNullConstant(binary.LeftOperand);

        if (leftIsNull == IsNullConstant(binary.RightOperand))
        {
            return false;
        }

        checkedOperation = OperationHelpers.Unwrap(leftIsNull ? binary.RightOperand : binary.LeftOperand);
        isNullWhenTrue = binary.OperatorKind == BinaryOperatorKind.Equals;

        return true;
    }

    private static bool TryReadSomeArgument(IOperation operation, out IOperation argument)
    {
        argument = null!;

        if (operation is not IInvocationOperation { Arguments.Length: 1 } invocation || !IsMaybeFactory(invocation.TargetMethod, "Some"))
        {
            return false;
        }

        argument = OperationHelpers.Unwrap(invocation.Arguments[0].Value);

        return true;
    }

    private static bool IsValueOfNullable(IOperation argument, IOperation checkedOperation) =>
        checkedOperation.Type is INamedTypeSymbol { OriginalDefinition.SpecialType: SpecialType.System_Nullable_T }
        && argument is IPropertyReferenceOperation { Property.Name: "Value", Instance: { } instance }
        && OperationHelpers.SameText(instance.Syntax, checkedOperation.Syntax);

    private static bool IsMaybeFactory(IMethodSymbol method, string name) =>
        method.Name == name
        && method.IsStatic
        && method.ContainingType is { Name: "Maybe", IsGenericType: false }
        && method.ContainingNamespace.ToDisplayString() == "Wyzely.Functional";

    private static bool IsNullConstant(IOperation operation) => OperationHelpers.Unwrap(operation).ConstantValue is { HasValue: true, Value: null };
}
