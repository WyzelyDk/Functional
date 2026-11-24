using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Wyzely.Functional.ResultAnalyzers;

/// <content>
/// The rule about <c>Ensure</c> with a negated condition (WYZ0105).
/// </content>
public sealed partial class ResultSuggestionsAnalyzer
{
    // WYZ0105: Ensure(x => !condition, error) is EnsureNot(x => condition, error), also with a factory for the error or its message.
    private static void CheckEnsureNot(OperationAnalysisContext context, IInvocationOperation invocation)
    {
        var hasError = OperationHelpers.ArgumentOf(invocation, "error") is not null
            || OperationHelpers.ArgumentOf(invocation, "message") is not null
            || OperationHelpers.ArgumentOf(invocation, "errorFactory") is not null
            || OperationHelpers.ArgumentOf(invocation, "messageFactory") is not null;

        if (hasError
            && OperationHelpers.TryGetLambda(OperationHelpers.ArgumentOf(invocation, "predicate"), 1, out var lambda, out var returned)
            && lambda.Symbol.ReturnType.SpecialType == SpecialType.System_Boolean
            && returned is IUnaryOperation { OperatorKind: UnaryOperatorKind.Not })
        {
            Report(context, Rules.UseEnsureNot, invocation, []);
        }
    }
}
