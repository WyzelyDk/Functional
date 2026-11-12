using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Wyzely.Functional.MaybeAnalyzers;

/// <summary>
/// Suggests the method of the library that says what a piece of code does: <c>Filter(p).IsSome</c> is <c>Any(p)</c>,
/// a conditional on <c>IsSome</c> or a <c>Match</c> that returns the value is <c>ValueOr</c>, and a null check that chooses between none and some is
/// <c>Maybe.From</c>; and points at code that does more than it has to: a <c>Bind</c> that only wraps in <c>Some</c>, a step that changes nothing,
/// and a fallback that is evaluated for nothing.
/// Every suggestion is an exact shape that can be rewritten without changing the behavior.
/// </summary>
/// <remarks>
/// A diagnostic carries a property (whether a fallback has to stay a function) so that the code fix,
/// which lives in another assembly, can rewrite the code from its syntax without asking the question again.
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed partial class MaybeSuggestionsAnalyzer : DiagnosticAnalyzer
{
    /// <summary>The property that tells whether a fallback has to stay a function, because evaluating it ahead of time would cost something.</summary>
    internal const string LazyProperty = "Lazy";

    private const string IsSomeName = "IsSome";

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
    [
        Rules.UseAny,
        Rules.UseValueOr,
        Rules.UseFrom,
        Rules.UseValueOrMatch,
        Rules.UseMapInsteadOfBind,
        Rules.RemoveNoOpStep,
        Rules.UseValueOrFunction,
    ];

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
        context.RegisterOperationAction(operation => AnalyzeInvocation(operation, types), OperationKind.Invocation);
    }

    // WYZ0202: maybe.Filter(p).IsSome is maybe.Any(p).
    private static void AnalyzeProperty(OperationAnalysisContext context, MaybeTypes types)
    {
        var property = (IPropertyReferenceOperation)context.Operation;

        if (property.Property.Name == IsSomeName
            && types.IsMaybe(property.Property.ContainingType)
            && property.Syntax is MemberAccessExpressionSyntax access
            && property.Instance is { } instance)
        {
            ReportFilterCall(context, access, instance);
        }
    }

    // `maybe.Filter(p)` written as a call of a member, read with IsSome.
    private static void ReportFilterCall(OperationAnalysisContext context, MemberAccessExpressionSyntax access, IOperation instance)
    {
        if (OperationHelpers.Unwrap(instance) is not IInvocationOperation { TargetMethod.Name: "Filter" } filter
            || !OperationShapes.IsMaybeExtension(filter.TargetMethod)
            || filter.Syntax is not InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax { Name: IdentifierNameSyntax } })
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(Rules.UseAny, access.Name.GetLocation()));
    }

    private static void AnalyzeConditional(OperationAnalysisContext context, MaybeTypes types)
    {
        var conditional = (IConditionalOperation)context.Operation;

        if (conditional.Syntax is not ConditionalExpressionSyntax)
        {
            return;
        }

        // A conditional expression, unlike an if statement, always has both branches.
        var otherwise = conditional.WhenFalse!;

        if (TryReadValueOrShape(conditional, otherwise, types, out var valueType, out var fallback))
        {
            CheckValueOrConditional(context, conditional, valueType, fallback);
        }
        else if (IsFromShape(conditional, otherwise, types))
        {
            Report(context, Rules.UseFrom, conditional);
        }
    }

    private static void AnalyzeInvocation(OperationAnalysisContext context, MaybeTypes types)
    {
        var invocation = (IInvocationOperation)context.Operation;
        var method = invocation.TargetMethod;

        if (OperationShapes.IsMaybeExtension(method))
        {
            AnalyzeExtension(context, invocation, types);
        }
        else if (method.Name == "ValueOr" && types.IsMaybe(method.ContainingType))
        {
            CheckValueOrWork(context, invocation);
        }
    }

    // The railway methods of the library that have a simpler form.
    private static void AnalyzeExtension(OperationAnalysisContext context, IInvocationOperation invocation, MaybeTypes types)
    {
        var name = invocation.TargetMethod.Name;

        if (name == "Match")
        {
            CheckMatchToValueOr(context, invocation, types);
        }
        else if (name == "Bind")
        {
            CheckBindToMap(context, invocation);
            CheckNoOpBind(context, invocation);
        }
        else if (name == "Map")
        {
            CheckNoOpMap(context, invocation);
        }
        else if (name == "OrElse")
        {
            CheckNoOpOrElse(context, invocation);
        }
        else if (name == "ValueOr")
        {
            CheckValueOrWork(context, invocation);
        }
    }

    // WYZ0204: x is null ? Maybe.None<T>() : Maybe.Some(x) is Maybe.From(x), also for a nullable value type with Some(x.Value).
    private static bool IsFromShape(IConditionalOperation conditional, IOperation otherwise, MaybeTypes types)
    {
        if (!OperationShapes.TryReadNullCheck(conditional.Condition, out var checkedOperation, out var isNullWhenTrue)
            || !OperationHelpers.IsPlain(checkedOperation.Syntax))
        {
            return false;
        }

        var none = OperationHelpers.Unwrap(isNullWhenTrue ? conditional.WhenTrue : otherwise);
        var some = OperationHelpers.Unwrap(isNullWhenTrue ? otherwise : conditional.WhenTrue);

        return OperationShapes.IsNone(none, types) && OperationShapes.IsSomeOf(some, checkedOperation);
    }

    private static void Report(
        OperationAnalysisContext context,
        DiagnosticDescriptor rule,
        IOperation operation,
        params KeyValuePair<string, string?>[] properties) =>
        context.ReportDiagnostic(Diagnostic.Create(rule, OperationHelpers.LocationOf(operation), properties.ToImmutableDictionary()));
}
