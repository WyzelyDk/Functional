using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Wyzely.Functional.ResultAnalyzers;

/// <content>
/// The rules about steps that only deal with the error: <c>MapError</c> instead of <c>OrElse</c> (WYZ0118),
/// and the steps that leave the error as it is, which are reported as steps that do nothing (WYZ0115).
/// </content>
public sealed partial class ResultSuggestionsAnalyzer
{
    // WYZ0118: OrElse(e => error) and OrElse(e => Result.Failure(error)) are MapError(e => error).
    // WYZ0115: when the error is the one the function was given, the step does nothing.
    // Only the function named alternative is looked at: the function named replacement returns the value, which for a Result<Error> is an error too.
    private static void CheckOrElse(OperationAnalysisContext context, IInvocationOperation invocation)
    {
        if (!IsPlainMemberCall(invocation) || !TryGetErrorOnlyFunction(invocation, out var function, out var error, out var wrapped))
        {
            return;
        }

        if (function.Symbol.Parameters.Length == 1 && OperationShapes.IsParameter(error, function.Symbol.Parameters[0]))
        {
            Report(context, Rules.RemoveNoOpStep, invocation, []);
        }
        else
        {
            Report(context, Rules.UseMapError, invocation, [Flag(WrappedProperty, wrapped)]);
        }
    }

    // The function is not async, has no parameter or one (the error), and returns one expression: an Error, or Result.Failure of an Error.
    // The error reaches a function that returns a result through the implicit conversion from Error, which is why the type of the expression is what counts.
    private static bool TryGetErrorOnlyFunction(
        IInvocationOperation invocation,
        out IAnonymousFunctionOperation function,
        out IOperation error,
        out bool wrapped)
    {
        error = null!;
        wrapped = false;

        if (!OperationShapes.TryGetLambda(OperationShapes.ArgumentOf(invocation, "alternative"), out function, out var returned) || function.Symbol.IsAsync)
        {
            return false;
        }

        if (OperationShapes.IsError(returned.Type))
        {
            error = returned;

            return true;
        }

        // Result.Failure(error) and Result.Failure<T>(error), but not the overloads that take a message.
        if (returned is IInvocationOperation { Arguments.Length: 1 } failure
            && OperationShapes.IsResultFactory(failure.TargetMethod, "Failure")
            && OperationShapes.IsError(OperationHelpers.Unwrap(failure.Arguments[0].Value).Type))
        {
            error = OperationHelpers.Unwrap(failure.Arguments[0].Value);
            wrapped = true;

            return true;
        }

        return false;
    }

    // WYZ0115: MapError(e => e) leaves the error as it is.
    private static void CheckNoOpMapError(OperationAnalysisContext context, IInvocationOperation invocation)
    {
        if (IsPlainMemberCall(invocation)
            && OperationShapes.TryGetLambda(OperationShapes.ArgumentOf(invocation, "mapError"), 1, out var function, out var returned)
            && !function.Symbol.IsAsync
            && OperationShapes.IsParameter(returned, function.Symbol.Parameters[0]))
        {
            Report(context, Rules.RemoveNoOpStep, invocation, []);
        }
    }
}
