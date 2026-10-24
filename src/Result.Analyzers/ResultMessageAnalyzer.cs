using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Wyzely.Functional.ResultAnalyzers;

/// <summary>Finds the messages of errors that are built with string interpolation.</summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ResultMessageAnalyzer : DiagnosticAnalyzer
{
    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = [Rules.InterpolatedMessage];

    /// <inheritdoc />
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterOperationAction(AnalyzeInvocation, OperationKind.Invocation);
    }

    private static void AnalyzeInvocation(OperationAnalysisContext context)
    {
        var invocation = (IInvocationOperation)context.Operation;
        var method = invocation.TargetMethod;

        if (!OperationShapes.IsLibraryMember(method))
        {
            return;
        }

        foreach (var argument in invocation.Arguments)
        {
            if (argument.Parameter is { Name: "message" or "context", Type.SpecialType: SpecialType.System_String }
                && OperationHelpers.Unwrap(argument.Value) is IInterpolatedStringOperation { ConstantValue.HasValue: false } interpolated)
            {
                context.ReportDiagnostic(Diagnostic.Create(Rules.InterpolatedMessage, interpolated.Syntax.GetLocation(), method.Name));
            }
        }
    }
}
