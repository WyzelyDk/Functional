using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Wyzely.Functional.MaybeAnalyzers;

/// <summary>
/// Suggests the method of the library that says what a piece of code does: <c>Filter(p).IsSome</c> is <c>Any(p)</c>, a conditional on
/// <c>IsSome</c> is <c>ValueOr</c>, and a null check that chooses between none and some is <c>Maybe.From</c>. Every suggestion is an exact shape
/// that can be rewritten without changing the behavior.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class MaybeSuggestionsAnalyzer : DiagnosticAnalyzer
{
    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = [Rules.UseAny, Rules.UseValueOr, Rules.UseFrom];

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

        context.RegisterOperationAction(operation => AnalyzeProperty(operation, types), OperationKind.PropertyReference);
        context.RegisterOperationAction(operation => AnalyzeConditional(operation, types), OperationKind.Conditional);
    }

    // WYZ0202: maybe.Filter(p).IsSome is maybe.Any(p).
    private static void AnalyzeProperty(OperationAnalysisContext context, MaybeTypes types)
    {
        var property = (IPropertyReferenceOperation)context.Operation;

        if (property.Property.Name == "IsSome"
            && types.IsMaybe(property.Property.ContainingType)
            && property.Syntax is MemberAccessExpressionSyntax access
            && property.Instance is { } instance
            && OperationShapes.Unwrap(instance) is IInvocationOperation { TargetMethod.Name: "Filter" } filter
            && OperationShapes.IsMaybeExtension(filter.TargetMethod)
            && filter.Syntax is InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax { Name: IdentifierNameSyntax } })
        {
            context.ReportDiagnostic(Diagnostic.Create(Rules.UseAny, access.Name.GetLocation()));
        }
    }

    private static void AnalyzeConditional(OperationAnalysisContext context, MaybeTypes types)
    {
        var conditional = (IConditionalOperation)context.Operation;

        if (conditional.Syntax is not ConditionalExpressionSyntax || conditional.WhenFalse is not { } otherwise)
        {
            return;
        }

        if (IsValueOrShape(conditional, otherwise, types))
        {
            context.ReportDiagnostic(Diagnostic.Create(Rules.UseValueOr, conditional.Syntax.GetLocation()));
        }
        else if (IsFromShape(conditional, otherwise, types))
        {
            context.ReportDiagnostic(Diagnostic.Create(Rules.UseFrom, conditional.Syntax.GetLocation()));
        }
    }

    // WYZ0203: maybe.IsSome ? maybe.GetValue() : fallback, and maybe.IsNone ? fallback : maybe.GetValue(), are maybe.ValueOr(fallback).
    // ValueOr evaluates the fallback on every call, so it has to be cheap, and the receiver has to be read the same way in both places.
    private static bool IsValueOrShape(IConditionalOperation conditional, IOperation otherwise, MaybeTypes types)
    {
        if (OperationShapes.Unwrap(conditional.Condition) is not IPropertyReferenceOperation { Property.Name: "IsSome" or "IsNone" } check
            || !types.IsMaybe(check.Property.ContainingType)
            || check.Instance is not { } receiver
            || !OperationShapes.IsPlain(receiver.Syntax))
        {
            return false;
        }

        var some = check.Property.Name == "IsSome";
        var read = OperationShapes.Unwrap(some ? conditional.WhenTrue : otherwise);
        var fallback = some ? otherwise : conditional.WhenTrue;

        if (read is not IInvocationOperation { TargetMethod: { Name: "GetValue", Parameters.Length: 0 }, Instance: { } getter }
            || !OperationShapes.SameText(getter.Syntax, receiver.Syntax)
            || !OperationShapes.IsCheap(fallback))
        {
            return false;
        }

        return true;
    }

    // WYZ0204: x is null ? Maybe.None<T>() : Maybe.Some(x) is Maybe.From(x), also for a nullable value type with Some(x.Value).
    private static bool IsFromShape(IConditionalOperation conditional, IOperation otherwise, MaybeTypes types)
    {
        if (!OperationShapes.TryReadNullCheck(conditional.Condition, out var checkedOperation, out var isNullWhenTrue)
            || !OperationShapes.IsPlain(checkedOperation.Syntax))
        {
            return false;
        }

        var none = OperationShapes.Unwrap(isNullWhenTrue ? conditional.WhenTrue : otherwise);
        var some = OperationShapes.Unwrap(isNullWhenTrue ? otherwise : conditional.WhenTrue);

        return OperationShapes.IsNone(none, types) && OperationShapes.IsSomeOf(some, checkedOperation);
    }
}
