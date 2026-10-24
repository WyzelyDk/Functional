using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Wyzely.Functional.ResultAnalyzers;

/// <summary>
/// Suggests the method of the library that says what a piece of code does: a condition inside <c>Bind</c> is <c>BindIf</c>, a negated
/// <c>Ensure</c> is <c>EnsureNot</c>, and so on. Every suggestion is an exact shape that can be rewritten without changing the behavior.
/// </summary>
/// <remarks>
/// A diagnostic carries a few properties (whether the condition has to be negated, whether an error has to stay lazy) so that the code fix, which
/// lives in another assembly, can rewrite the code from its syntax without asking the questions again.
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed partial class ResultSuggestionsAnalyzer : DiagnosticAnalyzer
{
    /// <summary>The property that tells whether the condition has to be negated by the rewrite.</summary>
    internal const string NegateProperty = "Negate";

    /// <summary>
    /// The property that tells whether a condition reads the value of the lambda, which decides between <c>Func&lt;bool&gt;</c> and
    /// <c>Func&lt;T, bool&gt;</c>.
    /// </summary>
    internal const string UsesParameterProperty = "UsesParameter";

    /// <summary>The property that tells whether an error must stay lazy, because creating it ahead of time would cost something or change something.</summary>
    internal const string LazyProperty = "Lazy";

    /// <summary>The property that tells whether the error is given as a message text.</summary>
    internal const string MessageProperty = "Message";

    /// <summary>The property that names the method to use when a rule can suggest two, <c>SuccessIf</c> or <c>FailureIf</c>.</summary>
    internal const string MethodProperty = "Method";

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
    [
        Rules.UseBindIf,
        Rules.UseMapIf,
        Rules.UseEnsureNot,
        Rules.UseEnsureNotNull,
        Rules.UseBindZip,
        Rules.UseConvertFailure,
        Rules.UseSuccessIf,
        Rules.UseMapTry,
        Rules.UseMapInsteadOfBind,
        Rules.UseTapInsteadOfMap,
        Rules.UseValueOr,
        Rules.RemoveNoOpStep,
        Rules.UseFailureWithMessage,
        Rules.UseFlatten,
    ];

    /// <inheritdoc />
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterOperationAction(AnalyzeInvocation, OperationKind.Invocation);
        context.RegisterOperationAction(AnalyzeConditional, OperationKind.Conditional);
    }

    private static void AnalyzeInvocation(OperationAnalysisContext context)
    {
        var invocation = (IInvocationOperation)context.Operation;
        var method = invocation.TargetMethod;

        if (OperationShapes.IsResultExtension(method, "Bind"))
        {
            CheckBindIf(context, invocation);
            CheckEnsureNotNull(context, invocation);
            CheckBindZip(context, invocation);
            CheckMapTry(context, invocation);
            CheckBindToMap(context, invocation);
            CheckNoOpBind(context, invocation);
            CheckFlatten(context, invocation);
        }
        else if (OperationShapes.IsResultExtension(method, "Map"))
        {
            CheckMapIf(context, invocation);
            CheckMapToTap(context, invocation);
            CheckNoOpMap(context, invocation);
        }
        else if (OperationShapes.IsResultExtension(method, "Match"))
        {
            CheckMatchToValueOr(context, invocation);
        }
        else if (OperationShapes.IsResultExtension(method, "Ensure"))
        {
            CheckEnsureNot(context, invocation);
        }
        else if (OperationShapes.IsResultFactory(method, "Failure"))
        {
            CheckConvertFailure(context, invocation);
            CheckFailureWithMessage(context, invocation);
        }
    }

    private static void AnalyzeConditional(OperationAnalysisContext context)
    {
        var conditional = (IConditionalOperation)context.Operation;

        if (conditional.Syntax is ConditionalExpressionSyntax)
        {
            CheckSuccessIf(context, conditional);
        }
    }

    // The diagnostic is reported on the name of the method for a call that is written as a member access, so that the squiggle is short.
    private static Location LocationOf(IOperation operation) =>
        operation.Syntax is InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax access }
            ? access.Name.GetLocation()
            : operation.Syntax.GetLocation();

    private static void Report(
        OperationAnalysisContext context,
        DiagnosticDescriptor rule,
        IOperation operation,
        IEnumerable<KeyValuePair<string, string?>> properties,
        params object?[] messageArguments) =>
        context.ReportDiagnostic(Diagnostic.Create(rule, LocationOf(operation), properties.ToImmutableDictionary(), messageArguments));

    private static KeyValuePair<string, string?> Flag(string name, bool value) => new(name, value ? "true" : "false");
}
