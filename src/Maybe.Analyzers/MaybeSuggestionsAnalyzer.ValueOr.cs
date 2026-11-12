using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Wyzely.Functional.MaybeAnalyzers;

/// <content>
/// The rules about a fallback value: <c>ValueOr</c> instead of a conditional on <c>IsSome</c> (WYZ0203), <c>ValueOr</c> instead of <c>Match</c> (WYZ0205),
/// and a function for <c>ValueOr</c> when the fallback does work (WYZ0208).
/// </content>
public sealed partial class MaybeSuggestionsAnalyzer
{
    // WYZ0203: maybe.IsSome ? maybe.GetValue() : fallback, and maybe.IsNone ? fallback : maybe.GetValue(), are maybe.ValueOr(fallback).
    // ValueOr evaluates the fallback on every call, so a fallback that is not cheap is passed as a function, which is evaluated only for none.
    private static void CheckValueOrConditional(
        OperationAnalysisContext context,
        IConditionalOperation conditional,
        ITypeSymbol valueType,
        IOperation fallback)
    {
        var cheap = OperationHelpers.IsCheap(fallback);

        if (cheap || CanBeFunction(fallback, valueType))
        {
            Report(context, Rules.UseValueOr, conditional, OperationHelpers.Flag(LazyProperty, !cheap));
        }
    }

    // The conditional is `maybe.IsSome ? maybe.GetValue() : fallback` or `maybe.IsNone ? fallback : maybe.GetValue()` on a maybe that is read
    // the same way in both places, and it has the type of the value, or ValueOr(fallback) would not compile or would give another type.
    private static bool TryReadValueOrShape(
        IConditionalOperation conditional,
        IOperation otherwise,
        MaybeTypes types,
        out ITypeSymbol valueType,
        out IOperation fallback)
    {
        valueType = null!;
        fallback = null!;

        if (!TryReadPresenceCheck(conditional.Condition, types, out var check, out var receiver))
        {
            return false;
        }

        var some = check.Property.Name == IsSomeName;
        var read = OperationHelpers.Unwrap(some ? conditional.WhenTrue : otherwise);

        if (!IsGetValueOf(read, receiver) || !SymbolEqualityComparer.Default.Equals(conditional.Type, read.Type))
        {
            return false;
        }

        valueType = read.Type!;
        fallback = OperationHelpers.Unwrap(some ? otherwise : conditional.WhenTrue);

        return true;
    }

    // A function is not an option when the fallback has no type of its own, cannot be moved into a lambda, or when a lambda would be taken for the value.
    private static bool CanBeFunction(IOperation fallback, ITypeSymbol valueType) =>
        OperationHelpers.HasOwnType(fallback) && !OperationHelpers.CannotBeDeferred(fallback) && !OperationHelpers.LambdaConvertsTo(valueType);

    // `maybe.IsSome` or `maybe.IsNone` on a plain receiver.
    private static bool TryReadPresenceCheck(
        IOperation condition,
        MaybeTypes types,
        out IPropertyReferenceOperation check,
        out IOperation receiver)
    {
        check = null!;
        receiver = null!;

        if (OperationHelpers.Unwrap(condition) is not IPropertyReferenceOperation candidate
            || !IsPresenceProperty(candidate.Property, types)
            || candidate.Instance is not { } instance
            || !OperationHelpers.IsPlain(instance.Syntax))
        {
            return false;
        }

        check = candidate;
        receiver = instance;

        return true;
    }

    private static bool IsPresenceProperty(IPropertySymbol property, MaybeTypes types) =>
        property.Name is IsSomeName or "IsNone" && types.IsMaybe(property.ContainingType);

    private static bool IsGetValueOf(IOperation read, IOperation receiver) =>
        read is IInvocationOperation { TargetMethod: { Name: "GetValue", Parameters.Length: 0 }, Instance: { } getter }
        && OperationHelpers.SameText(getter.Syntax, receiver.Syntax);

    // WYZ0205: Match(v => v, () => fallback) is ValueOr(fallback), which computes the fallback only for none when it is passed as a function.
    // The Match of a maybe, or of a Task or ValueTask of one, is the one with two type arguments, and they must be the same type.
    // A cheap fallback gets ValueOr(fallback), which evaluates the fallback always. On a Task or a ValueTask a null or default literal has no safe form:
    // as an argument it binds to an overload that takes a function, and as the result of a function to the one whose function returns a Task.
    private static void CheckMatchToValueOr(OperationAnalysisContext context, IInvocationOperation invocation, MaybeTypes types)
    {
        if (!TryReadMatchFallback(invocation, out var fallback))
        {
            return;
        }

        var method = invocation.TargetMethod;
        var plain = types.IsMaybe(method.Parameters[0].Type);
        var lazy = !OperationHelpers.IsCheap(fallback);

        if (!plain && !OperationHelpers.HasNaturalType(fallback))
        {
            return;
        }

        if (!lazy || (OperationHelpers.HasOwnType(fallback) && FunctionBinds(method.TypeArguments[0], plain)))
        {
            Report(context, Rules.UseValueOrMatch, invocation, OperationHelpers.Flag(LazyProperty, lazy));
        }
    }

    // A function that is passed to ValueOr has to bind to an overload that takes a function. A plain maybe has ValueOr(value), which a lambda binds to
    // when the value can be a lambda. A Task or ValueTask of a maybe has an overload that takes the value too, which a lambda cannot bind to unless
    // the value is an object (or dynamic): then the call is ambiguous.
    private static bool FunctionBinds(ITypeSymbol valueType, bool plainReceiver) =>
        plainReceiver ? !OperationHelpers.LambdaConvertsTo(valueType) : !OperationHelpers.IsObject(valueType);

    // The call is written as receiver.Match(...), the two type arguments are the same type, the first delegate returns the value it is given,
    // and the second is a lambda without parameters that returns one expression: the fallback.
    private static bool TryReadMatchFallback(IInvocationOperation invocation, out IOperation fallback)
    {
        fallback = null!;

        return OperationHelpers.IsPlainMemberCall(invocation)
            && OperationHelpers.HasEqualTypeArguments(invocation.TargetMethod)
            && OperationHelpers.TryGetSynchronousLambda(invocation, "onSome", 1, out var some, out var value)
            && OperationHelpers.IsParameter(value, some.Symbol.Parameters[0])
            && OperationHelpers.TryGetSynchronousLambda(invocation, "onNone", 0, out _, out fallback);
    }

    // WYZ0208: ValueOr(fallback) evaluates the fallback also when there is a value. A fallback that calls a method or creates an object is better
    // passed as a function. The call that takes the value is the method of Maybe<T> itself and the extension for a Task or a ValueTask of a maybe.
    private static void CheckValueOrWork(OperationAnalysisContext context, IInvocationOperation invocation)
    {
        var method = invocation.TargetMethod;

        if (!TakesTheValue(method))
        {
            return;
        }

        var fallback = OperationHelpers.ArgumentOf(invocation, "fallback")!;

        if (OperationHelpers.IsPlainMemberCall(invocation)
            && OperationHelpers.DoesWork(fallback)
            && CanBeFunction(fallback, method.Parameters[method.Parameters.Length - 1].Type))
        {
            Report(context, Rules.UseValueOrFunction, invocation);
        }
    }

    // The overload that takes the value: its parameter is the type parameter, where the others take a function.
    private static bool TakesTheValue(IMethodSymbol method) =>
        method.OriginalDefinition.Parameters[method.Parameters.Length - 1].Type is ITypeParameterSymbol;
}
