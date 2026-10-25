using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Wyzely.Functional.ResultAnalyzers;

/// <summary>
/// Finds the ways a <c>Result</c> is commonly misused: throwing one away, and reading the error or value without checking the state.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ResultUsageAnalyzer : DiagnosticAnalyzer
{
    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = [Rules.UnusedResult, Rules.UncheckedAccess];

    /// <inheritdoc />
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction(OnCompilationStart);
    }

    private static void OnCompilationStart(CompilationStartAnalysisContext context)
    {
        var types = ResultTypes.Create(context.Compilation);

        if (types is null)
        {
            return;
        }

        context.RegisterOperationAction(operation => AnalyzeExpressionStatement(operation, types), OperationKind.ExpressionStatement);
        context.RegisterOperationAction(operation => AnalyzeInvocation(operation, types), OperationKind.Invocation);
    }

    private static void AnalyzeInvocation(OperationAnalysisContext context, ResultTypes types)
    {
        var invocation = (IInvocationOperation)context.Operation;
        var method = invocation.TargetMethod;

        if (!ReadsState(method, types, out var needsFailure)
            || invocation.Syntax is not InvocationExpressionSyntax call
            || AccessGuards.IsGuarded(call, needsFailure))
        {
            return;
        }

        var flag = needsFailure ? "IsFailure" : "IsSuccess";
        var alternatives = needsFailure ? "TryGetError or Match" : "TryGetValue, ValueOr or Match";
        var state = needsFailure ? "failed" : "successful";

        context.ReportDiagnostic(Diagnostic.Create(Rules.UncheckedAccess, call.GetLocation(), method.Name + "()", state, flag, alternatives));
    }

    // GetError(), ConvertFailure() (which need a failed result) and GetValue() (which needs a successful one) of a result.
    private static bool ReadsState(IMethodSymbol method, ResultTypes types, out bool needsFailure)
    {
        needsFailure = method.Name is "GetError" or "ConvertFailure";

        return method.Parameters.Length == 0 && types.IsResult(method.ContainingType) && (needsFailure || method.Name == "GetValue");
    }

    private static void AnalyzeExpressionStatement(OperationAnalysisContext context, ResultTypes types)
    {
        var statement = (IExpressionStatementOperation)context.Operation;
        var expression = statement.Operation;

        if (expression is IAssignmentOperation || expression.Type is null)
        {
            return;
        }

        if (!types.IsResult(expression.Type) && !types.IsAwaitableResult(expression.Type))
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(Rules.UnusedResult, expression.Syntax.GetLocation(), Describe(expression)));
    }

    private static string Describe(IOperation expression)
    {
        var inner = expression is IAwaitOperation awaited ? awaited.Operation : expression;

        return inner is IInvocationOperation invocation ? invocation.TargetMethod.Name : inner.Syntax.ToString();
    }
}
