using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Wyzely.Functional.ResultAnalyzers;

/// <content>
/// The rule about <c>Ensure</c> with a negated condition (WYZ0105).
/// </content>
public sealed partial class ResultSuggestionsAnalyzer
{
    // WYZ0105: Ensure(x => !condition, error) is EnsureNot(x => condition, error). Only the forms with an error or a message exist for EnsureNot.
    private static void CheckEnsureNot(OperationAnalysisContext context, IInvocationOperation invocation)
    {
        var hasError = OperationShapes.ArgumentOf(invocation, "error") is not null || OperationShapes.ArgumentOf(invocation, "message") is not null;

        if (hasError
            && OperationShapes.TryGetLambda(OperationShapes.ArgumentOf(invocation, "predicate"), 1, out var lambda, out var returned)
            && lambda.Symbol.ReturnType.SpecialType == SpecialType.System_Boolean
            && returned is IUnaryOperation { OperatorKind: UnaryOperatorKind.Not })
        {
            Report(context, Rules.UseEnsureNot, invocation, []);
        }
    }
}
