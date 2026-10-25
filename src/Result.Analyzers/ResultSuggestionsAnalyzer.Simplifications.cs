using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
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
        if (!IsPlainMemberCall(invocation)
            || !TryGetSynchronousLambda(invocation, "bind", out var lambda, out var returned)
            || !TryGetTypedSuccess(returned, out var success))
        {
            return;
        }

        var value = OperationHelpers.Unwrap(success.Arguments[0].Value);

        if (!OperationShapes.IsParameter(value, lambda.Symbol.Parameters[0])
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

    // A lambda with one parameter that returns one expression and is not async.
    private static bool TryGetSynchronousLambda(
        IInvocationOperation invocation,
        string parameterName,
        out IAnonymousFunctionOperation lambda,
        out IOperation returned) =>
        OperationShapes.TryGetLambda(OperationShapes.ArgumentOf(invocation, parameterName), 1, out lambda, out returned) && !lambda.Symbol.IsAsync;

    // WYZ0113: Map(x => { Use(x); return x; }) is Tap(x => { Use(x); }). The statements before the return must not return, and must not change x.
    private static void CheckMapToTap(OperationAnalysisContext context, IInvocationOperation invocation)
    {
        if (!IsPlainMemberCall(invocation)
            || !HasEqualTypeArguments(invocation.TargetMethod)
            || !TryGetStatementLambda(invocation, out var lambda))
        {
            return;
        }

        var statements = lambda.Body.Operations;
        var parameter = lambda.Symbol.Parameters[0];

        if (statements[statements.Length - 1] is not IReturnOperation { ReturnedValue: { } returned }
            || !OperationShapes.IsParameter(returned, parameter)
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

        if (OperationShapes.ArgumentOf(invocation, "map") is not IDelegateCreationOperation { Target: IAnonymousFunctionOperation found }
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

    // WYZ0114: Match(v => v, _ => fallback) is ValueOr(fallback). ValueOr evaluates the fallback always, so it must be cheap and must not use the error.
    private static void CheckMatchToValueOr(OperationAnalysisContext context, IInvocationOperation invocation)
    {
        var method = invocation.TargetMethod;

        if (!IsMatchOfValueResult(method)
            || !TryGetSynchronousLambda(invocation, "onSuccess", out var success, out var value)
            || !TryGetSynchronousLambda(invocation, "onFailure", out var failure, out var fallback)
            || !OperationShapes.IsParameter(value, success.Symbol.Parameters[0]))
        {
            return;
        }

        if (OperationHelpers.IsCheap(fallback) && !OperationShapes.UsesParameter(fallback, failure.Symbol.Parameters[0]))
        {
            Report(context, Rules.UseValueOr, invocation, []);
        }
    }

    // A Match on a Result<T> whose two type arguments are the same type.
    private static bool IsMatchOfValueResult(IMethodSymbol method) =>
        method.Parameters[0].Type is INamedTypeSymbol { Name: "Result", IsGenericType: true } && HasEqualTypeArguments(method);

    // WYZ0115: Map(x => x) and Bind(x => Result.Success(x)) return the result they were called on.
    private static void CheckNoOpMap(OperationAnalysisContext context, IInvocationOperation invocation)
    {
        if (IsPlainMemberCall(invocation)
            && HasEqualTypeArguments(invocation.TargetMethod)
            && OperationShapes.TryGetLambda(OperationShapes.ArgumentOf(invocation, "map"), 1, out var lambda, out var returned)
            && !lambda.Symbol.IsAsync
            && OperationShapes.IsParameter(returned, lambda.Symbol.Parameters[0]))
        {
            Report(context, Rules.RemoveNoOpStep, invocation, []);
        }
    }

    private static void CheckNoOpBind(OperationAnalysisContext context, IInvocationOperation invocation)
    {
        if (IsPlainMemberCall(invocation)
            && OperationShapes.TryGetLambda(OperationShapes.ArgumentOf(invocation, "bind"), 1, out var lambda, out var returned)
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
        created = (OperationShapes.ArgumentOf(invocation, "error") as IInvocationOperation)!;

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
        if (IsPlainMemberCall(invocation)
            && OperationShapes.TryGetLambda(OperationShapes.ArgumentOf(invocation, "bind"), 1, out var lambda, out var returned)
            && !lambda.Symbol.IsAsync
            && lambda.Symbol.Parameters[0].Type is INamedTypeSymbol { Name: "Result", IsGenericType: true }
            && OperationShapes.IsParameter(returned, lambda.Symbol.Parameters[0]))
        {
            Report(context, Rules.UseFlatten, invocation, []);
        }
    }

    // The fixes rename the call, so it has to be written as member.Name(...) without type arguments.
    private static bool IsPlainMemberCall(IInvocationOperation invocation) =>
        invocation.Syntax is InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax { Name: IdentifierNameSyntax } };

    private static bool HasEqualTypeArguments(IMethodSymbol method) =>
        method.TypeArguments.Length == 2 && SymbolEqualityComparer.Default.Equals(method.TypeArguments[0], method.TypeArguments[1]);
}
