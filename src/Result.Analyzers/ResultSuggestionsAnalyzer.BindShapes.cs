using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Wyzely.Functional.ResultAnalyzers;

/// <content>
/// The rules about the other things a <c>Bind</c> delegate can be: <c>BindZip</c> (WYZ0107) and <c>MapTry</c> (WYZ0110).
/// </content>
public sealed partial class ResultSuggestionsAnalyzer
{
    // WYZ0107: Bind(a => f(a).Map(b => (a, b))) is BindZip(a => f(a)).
    private static void CheckBindZip(OperationAnalysisContext context, IInvocationOperation invocation)
    {
        if (!OperationShapes.TryGetLambda(OperationShapes.ArgumentOf(invocation, "bind"), 1, out var outer, out var returned)
            || returned is not IInvocationOperation map
            || !OperationShapes.IsResultExtension(map.TargetMethod, "Map")
            || !OperationShapes.TryGetLambda(OperationShapes.ArgumentOf(map, "map"), 1, out var inner, out var tuple)
            || tuple is not ITupleOperation { Elements.Length: 2 } pair)
        {
            return;
        }

        var first = outer.Symbol.Parameters[0];
        var second = inner.Symbol.Parameters[0];

        if (!OperationShapes.IsParameter(pair.Elements[0], first) || !OperationShapes.IsParameter(pair.Elements[1], second))
        {
            return;
        }

        // The tuple of the original has the elements named after the lambda parameters; BindZip names them First and Second. Only when the
        // very next call is a lambda that does not read those names is the rewrite sure to keep compiling.
        if (!NextCallIgnoresNames(invocation, first.Name, second.Name))
        {
            return;
        }

        Report(context, Rules.UseBindZip, invocation, []);
    }

    // WYZ0110: Bind(x => Result.Try(() => f(x))) is MapTry(x => f(x)). Try with a function: the forms without a value have no MapTry.
    private static void CheckMapTry(OperationAnalysisContext context, IInvocationOperation invocation)
    {
        if (OperationShapes.TryGetLambda(OperationShapes.ArgumentOf(invocation, "bind"), 1, out _, out var returned)
            && returned is IInvocationOperation attempt
            && OperationShapes.IsResultFactory(attempt.TargetMethod, "Try")
            && OperationShapes.ArgumentOf(attempt, "function") is { } function
            && OperationShapes.TryGetLambda(function, 0, out _, out _))
        {
            Report(context, Rules.UseMapTry, invocation, []);
        }
    }

    private static bool NextCallIgnoresNames(IInvocationOperation invocation, string first, string second)
    {
        if (invocation.Syntax.Parent is not MemberAccessExpressionSyntax { Parent: InvocationExpressionSyntax next })
        {
            return false;
        }

        return !next.DescendantNodes()
            .OfType<MemberAccessExpressionSyntax>()
            .Any(access => access.Name.Identifier.ValueText == first || access.Name.Identifier.ValueText == second);
    }
}
