using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Wyzely.Functional.ResultAnalyzers;

/// <content>
/// The rules about code that does more than it has to: <c>Map</c> instead of <c>Bind</c> (WYZ0112), <c>Tap</c> instead of <c>Map</c> (WYZ0113),
/// <c>ValueOr</c> instead of <c>Match</c> (WYZ0114), steps that do nothing (WYZ0115), <c>Failure</c> with a message (WYZ0116) and <c>Flatten</c> (WYZ0117).
/// </content>
public sealed partial class ResultSuggestionsAnalyzer
{
    // WYZ0112: Bind(x => Result.Success(f(x))) is Map(x => f(x)). The type of the value must stay the same, so Result.Success<object>(1) is left alone.
    private static void CheckBindToMap(OperationAnalysisContext context, IInvocationOperation invocation)
    {
        if (!OperationHelpers.IsPlainMemberCall(invocation)
            || !OperationHelpers.TryGetSynchronousLambda(invocation, "bind", 1, out var lambda, out var returned)
            || !TryGetTypedSuccess(returned, out var success))
        {
            return;
        }

        var value = OperationHelpers.Unwrap(success.Arguments[0].Value);

        if (!OperationHelpers.IsParameter(value, lambda.Symbol.Parameters[0])
            && SymbolEqualityComparer.Default.Equals(value.Type, success.TargetMethod.TypeArguments[0]))
        {
            Report(context, Rules.UseMapInsteadOfBind, invocation, []);
        }
    }

    // Result.Success<T>(value) with its type argument.
    private static bool TryGetTypedSuccess(IOperation operation, out IInvocationOperation success)
    {
        success = (operation as IInvocationOperation)!;

        return success is { Arguments.Length: 1 }
            && OperationShapes.IsResultFactory(success.TargetMethod, "Success")
            && success.TargetMethod.TypeArguments.Length == 1;
    }

    // WYZ0113: Map(x => { Use(x); return x; }) is Tap(x => { Use(x); }). The statements before the return must not return, and must not change x.
    private static void CheckMapToTap(OperationAnalysisContext context, IInvocationOperation invocation)
    {
        if (!OperationHelpers.IsPlainMemberCall(invocation)
            || !OperationHelpers.HasEqualTypeArguments(invocation.TargetMethod)
            || !TryGetStatementLambda(invocation, out var lambda))
        {
            return;
        }

        var statements = lambda.Body.Operations;
        var parameter = lambda.Symbol.Parameters[0];

        if (statements[statements.Length - 1] is not IReturnOperation { ReturnedValue: { } returned }
            || !OperationHelpers.IsParameter(returned, parameter)
            || AnyStatementEscapes(statements, parameter))
        {
            return;
        }

        Report(context, Rules.UseTapInsteadOfMap, invocation, []);
    }

    // The map is a lambda that is not async, has one parameter and has at least two statements: something to do and the return.
    private static bool TryGetStatementLambda(IInvocationOperation invocation, out IAnonymousFunctionOperation lambda)
    {
        lambda = null!;

        if (OperationHelpers.ArgumentOf(invocation, "map") is not IDelegateCreationOperation { Target: IAnonymousFunctionOperation found }
            || found.Symbol.IsAsync
            || found.Symbol.Parameters.Length != 1
            || found.Body.Operations.Length < 2)
        {
            return false;
        }

        lambda = found;

        return true;
    }

    // Any statement before the last that returns or may change the parameter.
    private static bool AnyStatementEscapes(ImmutableArray<IOperation> statements, IParameterSymbol parameter)
    {
        for (var index = 0; index < statements.Length - 1; index++)
        {
            if (OperationShapes.Contains<IReturnOperation>(statements[index]) || OperationShapes.ChangesParameter(statements[index], parameter))
            {
                return true;
            }
        }

        return false;
    }

    // WYZ0114: Match(v => v, onFailure) is ValueOr(onFailure), which computes the fallback only for a failure too.
    // The Match of a result with a value, on a result or on a Task or ValueTask of one, is the one with two type arguments, and they must be the same type.
    // When the fallback is cheap and does not use the error it is ValueOr(fallback), which evaluates the fallback always.
    private static void CheckMatchToValueOr(OperationAnalysisContext context, IInvocationOperation invocation)
    {
        var method = invocation.TargetMethod;

        if (!OperationHelpers.HasEqualTypeArguments(method)
            || !OperationHelpers.TryGetSynchronousLambda(invocation, "onSuccess", 1, out var success, out var value)
            || !OperationHelpers.TryGetSynchronousLambda(invocation, "onFailure", 1, out var failure, out var fallback)
            || !OperationHelpers.IsParameter(value, success.Symbol.Parameters[0]))
        {
            return;
        }

        // On a Task or a ValueTask a null or default literal has no safe form: as an argument it binds to an overload that takes a function,
        // and as the result of a function to the one whose function returns a Task.
        if (!OperationShapes.IsResultOfValue(method.Parameters[0].Type) && !OperationHelpers.HasNaturalType(fallback))
        {
            return;
        }

        var lazy = !OperationHelpers.IsCheap(fallback) || OperationShapes.UsesParameter(fallback, failure.Symbol.Parameters[0]);

        Report(context, Rules.UseValueOr, invocation, [OperationHelpers.Flag(LazyProperty, lazy)]);
    }

    // WYZ0115: Map(x => x) and Bind(x => Result.Success(x)) return the result they were called on.
    private static void CheckNoOpMap(OperationAnalysisContext context, IInvocationOperation invocation)
    {
        if (OperationHelpers.IsPlainMemberCall(invocation)
            && OperationHelpers.HasEqualTypeArguments(invocation.TargetMethod)
            && OperationHelpers.TryGetLambda(OperationHelpers.ArgumentOf(invocation, "map"), 1, out var lambda, out var returned)
            && !lambda.Symbol.IsAsync
            && OperationHelpers.IsParameter(returned, lambda.Symbol.Parameters[0]))
        {
            Report(context, Rules.RemoveNoOpStep, invocation, []);
        }
    }

    private static void CheckNoOpBind(OperationAnalysisContext context, IInvocationOperation invocation)
    {
        if (OperationHelpers.IsPlainMemberCall(invocation)
            && OperationHelpers.TryGetLambda(OperationHelpers.ArgumentOf(invocation, "bind"), 1, out var lambda, out var returned)
            && !lambda.Symbol.IsAsync
            && OperationShapes.IsSuccessOf(returned, lambda.Symbol.Parameters[0])
            && SymbolEqualityComparer.Default.Equals(((IInvocationOperation)returned).TargetMethod.TypeArguments[0], lambda.Symbol.Parameters[0].Type))
        {
            Report(context, Rules.RemoveNoOpStep, invocation, []);
        }
    }

    // WYZ0116: Result.Failure(Error.Create(message)) is Result.Failure(message). A null message would make the call ambiguous, so it is left alone.
    private static void CheckFailureWithMessage(OperationAnalysisContext context, IInvocationOperation invocation)
    {
        if (TryGetErrorCreate(invocation, out var created) && IsNotNullMessage(created.Arguments[0].Value))
        {
            Report(context, Rules.UseFailureWithMessage, invocation, []);
        }
    }

    // Error.Create(message) as the error of the call.
    private static bool TryGetErrorCreate(IInvocationOperation invocation, out IInvocationOperation created)
    {
        created = (OperationHelpers.ArgumentOf(invocation, "error") as IInvocationOperation)!;

        return created is { TargetMethod: { Name: "Create", IsStatic: true } create }
            && create.ContainingType.Name == "Error"
            && create.Parameters.Length == 1;
    }

    private static bool IsNotNullMessage(IOperation operation)
    {
        var message = OperationHelpers.Unwrap(operation);

        return !message.ConstantValue.HasValue || message.ConstantValue.Value is not null;
    }

    // WYZ0117: Bind(x => x) on a result of a result is Flatten().
    private static void CheckFlatten(OperationAnalysisContext context, IInvocationOperation invocation)
    {
        if (OperationHelpers.IsPlainMemberCall(invocation)
            && OperationHelpers.TryGetLambda(OperationHelpers.ArgumentOf(invocation, "bind"), 1, out var lambda, out var returned)
            && !lambda.Symbol.IsAsync
            && lambda.Symbol.Parameters[0].Type is INamedTypeSymbol { Name: "Result", IsGenericType: true }
            && OperationHelpers.IsParameter(returned, lambda.Symbol.Parameters[0]))
        {
            Report(context, Rules.UseFlatten, invocation, []);
        }
    }
}
