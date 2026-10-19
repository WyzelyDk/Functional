using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Wyzely.Functional.MaybeAnalyzers;

/// <summary>
/// Finds the way a <c>Maybe</c> is commonly misused: reading its value without checking that it has one. A default maybe is a valid
/// none and ignoring a maybe is harmless, so neither is reported.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class MaybeUsageAnalyzer : DiagnosticAnalyzer
{
    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Rules.UncheckedAccess);

    /// <inheritdoc />
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction(OnCompilationStart);
    }

    private static void OnCompilationStart(CompilationStartAnalysisContext context)
    {
        var types = MaybeTypes.Create(context.Compilation);

        if (types is null)
        {
            return;
        }

        context.RegisterOperationAction(operation => AnalyzeInvocation(operation, types), OperationKind.Invocation);
    }

    private static void AnalyzeInvocation(OperationAnalysisContext context, MaybeTypes types)
    {
        var invocation = (IInvocationOperation)context.Operation;
        var method = invocation.TargetMethod;

        if (method.Name != "GetValue" || method.Parameters.Length != 0 || !types.IsMaybe(method.ContainingType))
        {
            return;
        }

        if (invocation.Syntax is not InvocationExpressionSyntax call || AccessGuards.IsGuarded(call))
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(Rules.UncheckedAccess, call.GetLocation()));
    }
}
