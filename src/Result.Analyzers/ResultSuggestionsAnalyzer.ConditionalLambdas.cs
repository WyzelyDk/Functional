using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Wyzely.Functional.ResultAnalyzers;

/// <content>
/// The rules about a lambda that is a conditional expression: <c>BindIf</c>, <c>MapIf</c> and <c>EnsureNotNull</c>.
/// </content>
public sealed partial class ResultSuggestionsAnalyzer
{
    // WYZ0103: Bind(x => cond ? f(x) : Result.Success(x)), or the same with the branches the other way around.
    private static void CheckBindIf(OperationAnalysisContext context, IInvocationOperation invocation)
    {
        if (!TryGetConditionalLambda(invocation, "bind", out var lambda, out var conditional, out var whenTrue, out var whenFalse))
        {
            return;
        }

        var parameter = lambda.Symbol.Parameters[0];
        var whenTrueIsPass = OperationShapes.IsSuccessOf(whenTrue, parameter);
        var whenFalseIsPass = OperationShapes.IsSuccessOf(whenFalse, parameter);

        if (whenTrueIsPass == whenFalseIsPass)
        {
            return;
        }

        // A branch that is a failure is a validation, which Ensure and EnsureNotNull say better.
        var other = OperationShapes.Unwrap(whenTrueIsPass ? whenFalse : whenTrue);

        if (other is IInvocationOperation otherCall && OperationShapes.IsResultFactory(otherCall.TargetMethod, "Failure"))
        {
            return;
        }

        Report(
            context,
            Rules.UseBindIf,
            invocation,
            [Flag(NegateProperty, whenTrueIsPass), Flag(UsesParameterProperty, OperationShapes.UsesParameter(conditional.Condition, parameter))]);
    }

    // WYZ0104: Map(x => cond ? f(x) : x), or the same with the branches the other way around. Only when the other branch has the type of x.
    private static void CheckMapIf(OperationAnalysisContext context, IInvocationOperation invocation)
    {
        if (!TryGetConditionalLambda(invocation, "map", out var lambda, out var conditional, out var whenTrue, out var whenFalse))
        {
            return;
        }

        var parameter = lambda.Symbol.Parameters[0];
        var whenTrueIsPass = OperationShapes.IsParameter(whenTrue, parameter);
        var whenFalseIsPass = OperationShapes.IsParameter(whenFalse, parameter);

        if (whenTrueIsPass == whenFalseIsPass)
        {
            return;
        }

        var other = whenTrueIsPass ? whenFalse : whenTrue;

        if (!SymbolEqualityComparer.Default.Equals(OperationShapes.Unwrap(other).Type, parameter.Type))
        {
            return;
        }

        Report(
            context,
            Rules.UseMapIf,
            invocation,
            [Flag(NegateProperty, whenTrueIsPass), Flag(UsesParameterProperty, OperationShapes.UsesParameter(conditional.Condition, parameter))]);
    }

    // WYZ0106: Bind(x => x is null ? Result.Failure<T>(error) : Result.Success(x)), with == null, is not null and != null as well.
    private static void CheckEnsureNotNull(OperationAnalysisContext context, IInvocationOperation invocation)
    {
        if (!TryGetConditionalLambda(invocation, "bind", out var lambda, out var conditional, out var whenTrue, out var whenFalse))
        {
            return;
        }

        var parameter = lambda.Symbol.Parameters[0];
        var isNullWhenTrue = NullCheck(conditional.Condition, parameter);

        if (isNullWhenTrue is null || !(parameter.Type.IsReferenceType || IsNullableValueType(parameter.Type)))
        {
            return;
        }

        var failure = OperationShapes.Unwrap(isNullWhenTrue.Value ? whenTrue : whenFalse);
        var success = OperationShapes.Unwrap(isNullWhenTrue.Value ? whenFalse : whenTrue);

        if (!IsSuccessWithValue(success, parameter) || failure is not IInvocationOperation { Arguments.Length: 1 } failed)
        {
            return;
        }

        var method = failed.TargetMethod;
        var error = OperationShapes.ArgumentOf(failed, "error") ?? OperationShapes.ArgumentOf(failed, "message");

        if (!OperationShapes.IsResultFactory(method, "Failure") || method.TypeArguments.Length != 1 || error is null)
        {
            return;
        }

        Report(
            context,
            Rules.UseEnsureNotNull,
            invocation,
            [Flag(LazyProperty, !OperationShapes.IsCheap(error)), Flag(MessageProperty, error.Type?.SpecialType == SpecialType.System_String)]);
    }

    private static bool TryGetConditionalLambda(
        IInvocationOperation invocation,
        string parameterName,
        out IAnonymousFunctionOperation lambda,
        out IConditionalOperation conditional,
        out IOperation whenTrue,
        out IOperation whenFalse)
    {
        conditional = null!;
        whenTrue = null!;
        whenFalse = null!;

        if (OperationShapes.TryGetLambda(OperationShapes.ArgumentOf(invocation, parameterName), 1, out lambda, out var returned)
            && returned is IConditionalOperation { WhenFalse: { } otherwise } found)
        {
            conditional = found;
            whenTrue = found.WhenTrue;
            whenFalse = otherwise;

            return true;
        }

        return false;
    }

    // Returns true when the condition is true for null, false when it is true for not null, and null when it is not a null check of the parameter.
    private static bool? NullCheck(IOperation condition, IParameterSymbol parameter)
    {
        switch (OperationShapes.Unwrap(condition))
        {
            case IIsPatternOperation pattern when OperationShapes.IsParameter(pattern.Value, parameter):
                if (pattern.Pattern is IConstantPatternOperation constant && IsNull(constant.Value))
                {
                    return true;
                }

                return pattern.Pattern is INegatedPatternOperation { Pattern: IConstantPatternOperation negated } && IsNull(negated.Value) ? false : null;

            case IBinaryOperation { OperatorKind: BinaryOperatorKind.Equals or BinaryOperatorKind.NotEquals } binary:
                var equals = binary.OperatorKind == BinaryOperatorKind.Equals;

                if (OperationShapes.IsParameter(binary.LeftOperand, parameter) && IsNull(binary.RightOperand))
                {
                    return equals;
                }

                return OperationShapes.IsParameter(binary.RightOperand, parameter) && IsNull(binary.LeftOperand) ? equals : null;

            default:
                return null;
        }
    }

    private static bool IsNull(IOperation operation) =>
        OperationShapes.Unwrap(operation).ConstantValue is { HasValue: true, Value: null };

    private static bool IsNullableValueType(ITypeSymbol type) => type is INamedTypeSymbol { OriginalDefinition.SpecialType: SpecialType.System_Nullable_T };

    // Result.Success(x) for a reference type, and Result.Success(x.Value) for a nullable value type.
    private static bool IsSuccessWithValue(IOperation operation, IParameterSymbol parameter)
    {
        if (OperationShapes.IsSuccessOf(operation, parameter))
        {
            return true;
        }

        return operation is IInvocationOperation { Arguments.Length: 1 } call
            && OperationShapes.IsResultFactory(call.TargetMethod, "Success")
            && OperationShapes.Unwrap(call.Arguments[0].Value) is IPropertyReferenceOperation { Property.Name: "Value", Instance: { } instance }
            && OperationShapes.IsParameter(instance, parameter);
    }
}
