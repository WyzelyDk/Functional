using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Wyzely.Functional.ResultAnalyzers;

/// <content>
/// The rules about a fallback value: <c>ValueOr</c> instead of a conditional on the state of a result (WYZ0119),
/// and a function for <c>ValueOr</c> when the fallback does work (WYZ0120).
/// </content>
public sealed partial class ResultSuggestionsAnalyzer
{
    // WYZ0119: result.IsSuccess ? result.GetValue() : fallback, and result.IsFailure ? fallback : result.GetValue(), are result.ValueOr(fallback).
    // ValueOr evaluates the fallback on every call, so a fallback that is not cheap is passed as a function, which is evaluated only for a failure.
    // A fallback that reads result.GetError() is passed as a function of the error.
    private static void CheckValueOrConditional(OperationAnalysisContext context, IConditionalOperation conditional)
    {
        if (!TryReadValueOrShape(conditional, out var receiver, out var valueType, out var fallback))
        {
            return;
        }

        var cheap = OperationHelpers.IsCheap(fallback);
        var usesError = OperationShapes.ReadsErrorOf(fallback, receiver);

        if (!cheap && !CanBeFunction(conditional, fallback, valueType, usesError))
        {
            return;
        }

        Report(context, Rules.UseValueOrConditional, conditional, [Flag(LazyProperty, !cheap), Flag(UsesErrorProperty, usesError)]);
    }

    // The conditional is `result.IsSuccess ? result.GetValue() : fallback` or `result.IsFailure ? fallback : result.GetValue()` on a result that is read
    // the same way in both places, and it has the type of the value, or ValueOr(fallback) would not compile or would give another type.
    private static bool TryReadValueOrShape(IConditionalOperation conditional, out IOperation receiver, out ITypeSymbol valueType, out IOperation fallback)
    {
        valueType = null!;
        fallback = null!;

        if (!TryReadStateCheck(conditional.Condition, out var success, out receiver))
        {
            return false;
        }

        // A conditional expression, unlike an if statement, always has both branches.
        var otherwise = conditional.WhenFalse!;
        var read = OperationHelpers.Unwrap(success ? conditional.WhenTrue : otherwise);

        if (!IsGetValueOf(read, receiver) || !SymbolEqualityComparer.Default.Equals(conditional.Type, read.Type))
        {
            return false;
        }

        valueType = read.Type!;
        fallback = OperationHelpers.Unwrap(success ? otherwise : conditional.WhenTrue);

        return true;
    }

    // A function is not an option when the fallback throws or cannot be moved into a lambda, when a lambda would be taken for the value, or when the name
    // of the parameter that takes the error is already used.
    private static bool CanBeFunction(IConditionalOperation conditional, IOperation fallback, ITypeSymbol valueType, bool usesError)
    {
        if (fallback is IThrowOperation || OperationShapes.CannotBeDeferred(fallback) || OperationShapes.LambdaConvertsTo(valueType))
        {
            return false;
        }

        return !usesError || !IsErrorNameTaken(conditional, fallback);
    }

    // `result.IsSuccess` or `result.IsFailure` on a result with a value that is read the same way in every place that it is written.
    private static bool TryReadStateCheck(IOperation condition, out bool success, out IOperation receiver)
    {
        success = false;
        receiver = null!;

        if (OperationHelpers.Unwrap(condition) is not IPropertyReferenceOperation { Instance: { } instance } check
            || !IsStateProperty(check.Property)
            || !OperationHelpers.IsPlain(instance.Syntax))
        {
            return false;
        }

        success = check.Property.Name == "IsSuccess";
        receiver = instance;

        return true;
    }

    private static bool IsStateProperty(IPropertySymbol property) =>
        property.Name is "IsSuccess" or "IsFailure" && OperationShapes.IsResultOfValue(property.ContainingType);

    private static bool IsGetValueOf(IOperation read, IOperation receiver) =>
        read is IInvocationOperation { TargetMethod.Name: "GetValue", Instance: { } getter }
        && OperationHelpers.SameText(getter.Syntax, receiver.Syntax);

    // The function takes the error as `error`, which must not hide or clash with anything of the same name: a symbol in scope where the conditional is,
    // or a name used inside the fallback (a parameter of a lambda in it, for example).
    private static bool IsErrorNameTaken(IConditionalOperation conditional, IOperation fallback) =>
        conditional.SemanticModel!.LookupSymbols(conditional.Syntax.SpanStart, name: "error").Length > 0
        || fallback.Syntax.DescendantTokens().Any(token => token.ValueText == "error");

    // WYZ0120: ValueOr(fallback) evaluates the fallback also for a success. A fallback that calls a method or creates an object is better passed as a function.
    private static void CheckValueOrWork(OperationAnalysisContext context, IInvocationOperation invocation)
    {
        var method = invocation.TargetMethod;

        if (!(OperationShapes.IsResultExtension(method, "ValueOr") || OperationShapes.IsResultOfValue(method.ContainingType))
            || !IsPlainMemberCall(invocation))
        {
            return;
        }

        // The overload that takes the value: its parameter is the type parameter, where the others take a function.
        var parameters = method.OriginalDefinition.Parameters;

        if (parameters[parameters.Length - 1].Type is not ITypeParameterSymbol)
        {
            return;
        }

        var fallback = OperationShapes.ArgumentOf(invocation, "fallback")!;

        if (OperationShapes.DoesWork(fallback)
            && !OperationShapes.CannotBeDeferred(fallback)
            && !OperationShapes.LambdaConvertsTo(method.Parameters[method.Parameters.Length - 1].Type))
        {
            Report(context, Rules.UseValueOrFunction, invocation, []);
        }
    }
}
