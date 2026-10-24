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
            || !OperationShapes.TryGetLambda(OperationShapes.ArgumentOf(invocation, "bind"), 1, out var lambda, out var returned)
            || lambda.Symbol.IsAsync
            || returned is not IInvocationOperation { Arguments.Length: 1 } success
            || !OperationShapes.IsResultFactory(success.TargetMethod, "Success")
            || success.TargetMethod.TypeArguments.Length != 1)
        {
            return;
        }

        var value = OperationShapes.Unwrap(success.Arguments[0].Value);

        if (!OperationShapes.IsParameter(value, lambda.Symbol.Parameters[0])
            && SymbolEqualityComparer.Default.Equals(value.Type, success.TargetMethod.TypeArguments[0]))
        {
            Report(context, Rules.UseMapInsteadOfBind, invocation, []);
        }
    }

    // WYZ0113: Map(x => { Use(x); return x; }) is Tap(x => { Use(x); }). The statements before the return must not return, and must not change x.
    private static void CheckMapToTap(OperationAnalysisContext context, IInvocationOperation invocation)
    {
        if (!IsPlainMemberCall(invocation)
            || !HasEqualTypeArguments(invocation.TargetMethod)
            || OperationShapes.ArgumentOf(invocation, "map") is not IDelegateCreationOperation { Target: IAnonymousFunctionOperation lambda }
            || lambda.Symbol.IsAsync
            || lambda.Symbol.Parameters.Length != 1
            || lambda.Body.Operations.Length < 2)
        {
            return;
        }

        var statements = lambda.Body.Operations;
        var parameter = lambda.Symbol.Parameters[0];

        if (statements[statements.Length - 1] is not IReturnOperation { ReturnedValue: { } returned } || !OperationShapes.IsParameter(returned, parameter))
        {
            return;
        }

        for (var index = 0; index < statements.Length - 1; index++)
        {
            if (OperationShapes.Contains<IReturnOperation>(statements[index]) || OperationShapes.ChangesParameter(statements[index], parameter))
            {
                return;
            }
        }

        Report(context, Rules.UseTapInsteadOfMap, invocation, []);
    }

    // WYZ0114: Match(v => v, _ => fallback) is ValueOr(fallback). ValueOr evaluates the fallback always, so it must be cheap and must not use the error.
    private static void CheckMatchToValueOr(OperationAnalysisContext context, IInvocationOperation invocation)
    {
        var method = invocation.TargetMethod;

        if (method.Parameters[0].Type is not INamedTypeSymbol { Name: "Result", IsGenericType: true }
            || !HasEqualTypeArguments(method)
            || !OperationShapes.TryGetLambda(OperationShapes.ArgumentOf(invocation, "onSuccess"), 1, out var success, out var value)
            || !OperationShapes.TryGetLambda(OperationShapes.ArgumentOf(invocation, "onFailure"), 1, out var failure, out var fallback)
            || success.Symbol.IsAsync
            || failure.Symbol.IsAsync
            || !OperationShapes.IsParameter(value, success.Symbol.Parameters[0]))
        {
            return;
        }

        if (OperationShapes.IsCheap(fallback) && !OperationShapes.UsesParameter(fallback, failure.Symbol.Parameters[0]))
        {
            Report(context, Rules.UseValueOr, invocation, []);
        }
    }

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
        if (OperationShapes.ArgumentOf(invocation, "error") is IInvocationOperation { TargetMethod: { Name: "Create", IsStatic: true } create } created
            && create.ContainingType.Name == "Error"
            && create.Parameters.Length == 1)
        {
            var message = OperationShapes.Unwrap(created.Arguments[0].Value);

            if (!message.ConstantValue.HasValue || message.ConstantValue.Value is not null)
            {
                Report(context, Rules.UseFailureWithMessage, invocation, []);
            }
        }
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
