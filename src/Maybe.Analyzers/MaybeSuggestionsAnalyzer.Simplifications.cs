using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Wyzely.Functional.MaybeAnalyzers;

/// <content>
/// The rules about code that does more than it has to: <c>Map</c> instead of <c>Bind</c> (WYZ0206) and steps that do nothing (WYZ0207).
/// </content>
public sealed partial class MaybeSuggestionsAnalyzer
{
    // WYZ0206: Bind(x => Maybe.Some(f(x))) is Map(x => f(x)). Some throws for null and Map turns null into none, so only a value that can never be null
    // may change from the one to the other: a value of a value type, which is not a nullable one. The type of the value must stay the same too,
    // so Maybe.Some<object>(1) is left alone. A Bind(x => Maybe.Some(x)) does nothing, which is WYZ0207.
    private static void CheckBindToMap(OperationAnalysisContext context, IInvocationOperation invocation)
    {
        if (!OperationHelpers.IsPlainMemberCall(invocation)
            || !OperationHelpers.TryGetSynchronousLambda(invocation, "bind", 1, out var lambda, out var returned)
            || !OperationShapes.TryReadSome(returned, out var some))
        {
            return;
        }

        var value = OperationHelpers.Unwrap(some.Arguments[0].Value);

        if (!OperationHelpers.IsParameter(value, lambda.Symbol.Parameters[0])
            && SymbolEqualityComparer.Default.Equals(value.Type, some.TargetMethod.TypeArguments[0])
            && OperationShapes.IsNeverNull(value.Type))
        {
            Report(context, Rules.UseMapInsteadOfBind, invocation);
        }
    }

    // WYZ0207: Map(x => x), Bind(x => Maybe.Some(x)) and OrElse(() => Maybe.None<T>()), or with default(Maybe<T>), return the maybe they were called on.
    private static void CheckNoOpMap(OperationAnalysisContext context, IInvocationOperation invocation)
    {
        if (OperationHelpers.IsPlainMemberCall(invocation)
            && OperationHelpers.HasEqualTypeArguments(invocation.TargetMethod)
            && OperationHelpers.TryGetSynchronousLambda(invocation, "map", 1, out var lambda, out var returned)
            && OperationHelpers.IsParameter(returned, lambda.Symbol.Parameters[0]))
        {
            Report(context, Rules.RemoveNoOpStep, invocation);
        }
    }

    private static void CheckNoOpBind(OperationAnalysisContext context, IInvocationOperation invocation)
    {
        if (OperationHelpers.IsPlainMemberCall(invocation)
            && OperationHelpers.TryGetSynchronousLambda(invocation, "bind", 1, out var lambda, out var returned)
            && OperationShapes.TryReadSome(returned, out var some)
            && OperationHelpers.IsParameter(some.Arguments[0].Value, lambda.Symbol.Parameters[0])
            && SymbolEqualityComparer.Default.Equals(some.TargetMethod.TypeArguments[0], lambda.Symbol.Parameters[0].Type))
        {
            Report(context, Rules.RemoveNoOpStep, invocation);
        }
    }

    // Only the function named alternative is looked at: the function named replacement returns a value, which is never a call of Maybe.None.
    private static void CheckNoOpOrElse(OperationAnalysisContext context, IInvocationOperation invocation)
    {
        if (OperationHelpers.IsPlainMemberCall(invocation)
            && OperationHelpers.TryGetSynchronousLambda(invocation, "alternative", 0, out _, out var returned)
            && IsNoneOf(returned, invocation.TargetMethod.TypeArguments[0]))
        {
            Report(context, Rules.RemoveNoOpStep, invocation);
        }
    }

    // Maybe.None<T>() of the value type of the receiver, or default(Maybe<T>): the lambda returns a maybe of that type,
    // so a default is the one of the receiver.
    private static bool IsNoneOf(IOperation returned, ITypeSymbol valueType) =>
        returned is IDefaultValueOperation
        || (OperationShapes.TryReadNone(returned, out var none) && SymbolEqualityComparer.Default.Equals(none.TargetMethod.TypeArguments[0], valueType));
}
