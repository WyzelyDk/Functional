using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Wyzely.Functional.ResultAnalyzers;

/// <content>
/// The rules about creating results: <c>ConvertFailure</c> (WYZ0108) and <c>SuccessIf</c> or <c>FailureIf</c> (WYZ0109).
/// </content>
public sealed partial class ResultSuggestionsAnalyzer
{
    // WYZ0108: Result.Failure<T>(r.GetError()) is r.ConvertFailure<T>().
    private static void CheckConvertFailure(OperationAnalysisContext context, IInvocationOperation invocation)
    {
        if (invocation.TargetMethod.TypeArguments.Length == 1
            && OperationShapes.ArgumentOf(invocation, "error") is IInvocationOperation { Arguments.Length: 0, Instance: not null } getError
            && getError.TargetMethod is { Name: "GetError", ContainingType.Name: "Result" })
        {
            Report(context, Rules.UseConvertFailure, invocation, []);
        }
    }

    // WYZ0109: cond ? Result.Success() : Result.Failure(error) is Result.SuccessIf(cond, error), and the reverse is Result.FailureIf(cond, error).
    private static void CheckSuccessIf(OperationAnalysisContext context, IConditionalOperation conditional)
    {
        var whenTrue = OperationHelpers.Unwrap(conditional.WhenTrue);

        // A conditional expression, unlike an if statement, always has both branches.
        var whenFalse = OperationHelpers.Unwrap(conditional.WhenFalse!);
        var successFirst = IsSuccessWithoutValue(whenTrue);

        if (successFirst == IsSuccessWithoutValue(whenFalse))
        {
            return;
        }

        var failure = successFirst ? whenFalse : whenTrue;

        // The branch has the type Result, so a failure with a value type (Result<T>) cannot be the other branch.
        if (failure is not IInvocationOperation { Arguments.Length: 1 } failed || !OperationShapes.IsResultFactory(failed.TargetMethod, "Failure"))
        {
            return;
        }

        var error = (OperationShapes.ArgumentOf(failed, "error") ?? OperationShapes.ArgumentOf(failed, "message"))!;

        // Passing the error as a function would allocate a delegate on every call, so an error that is not a constant, a local or a field stays as it is.
        if (!OperationHelpers.IsCheap(error))
        {
            return;
        }

        var method = successFirst ? "SuccessIf" : "FailureIf";

        Report(
            context,
            Rules.UseSuccessIf,
            conditional,
            [new(MethodProperty, method)],
            method);
    }

    private static bool IsSuccessWithoutValue(IOperation operation) =>
        operation is IInvocationOperation { Arguments.Length: 0 } call && OperationShapes.IsResultFactory(call.TargetMethod, "Success");
}
