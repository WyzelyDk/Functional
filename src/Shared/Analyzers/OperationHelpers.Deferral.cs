using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;

namespace Wyzely.Functional;

/// <content>Telling whether an expression can be moved into a lambda, and whether a lambda can be passed for a value of a type.</content>
internal static partial class OperationHelpers
{
    /// <summary>
    /// Determines whether a lambda can be converted to a type, so that a call that is given a lambda for a parameter of that type may bind to a
    /// value parameter instead of a function parameter: <c>object</c>, <c>dynamic</c>, <c>Delegate</c>, <c>MulticastDelegate</c>, a delegate type
    /// and an expression tree.
    /// </summary>
    /// <param name="type">The type of the value.</param>
    /// <returns><see langword="true"/> when a lambda would be taken for a value of the type.</returns>
    internal static bool LambdaConvertsTo(ITypeSymbol type) =>
        type.TypeKind == TypeKind.Delegate
        || IsObject(type)
        || type.SpecialType is SpecialType.System_Delegate or SpecialType.System_MulticastDelegate
        || IsExpressionTree(type);

    /// <summary>Determines whether a type is <c>object</c> or <c>dynamic</c>, which every type converts to.</summary>
    /// <param name="type">The type to test.</param>
    /// <returns><see langword="true"/> for <c>object</c> and <c>dynamic</c>.</returns>
    internal static bool IsObject(ITypeSymbol type) => type.TypeKind == TypeKind.Dynamic || type.SpecialType == SpecialType.System_Object;

    /// <summary>
    /// Determines whether an operation does work that costs something when it is evaluated for nothing: a call of a method,
    /// the creation of an object, an array or a collection, or an interpolated string that is not a constant.
    /// Reading a local, a parameter, a field, a property or a constant is not work.
    /// </summary>
    /// <param name="operation">The operation to search.</param>
    /// <returns><see langword="true"/> when the operation or an operation inside it does work.</returns>
    internal static bool DoesWork(IOperation operation) =>
        operation.DescendantsAndSelf().Any(
            node => node is IInvocationOperation
                or IObjectCreationOperation
                or IArrayCreationOperation
                or ICollectionExpressionOperation
                or IInterpolatedStringOperation { ConstantValue.HasValue: false });

    /// <summary>
    /// Determines whether an operation cannot be moved into a lambda: it awaits, declares a pattern variable, passes something by <see langword="ref"/> or
    /// <see langword="out"/>, or reads a <see langword="ref"/>, <see langword="out"/> or <see langword="in"/> parameter, a <see langword="ref"/> local,
    /// a value of a <see langword="ref"/> struct, or the instance of a struct, none of which a lambda can capture.
    /// </summary>
    /// <param name="operation">The operation to search.</param>
    /// <returns><see langword="true"/> when something inside the operation stops it from being the body of a lambda.</returns>
    internal static bool CannotBeDeferred(IOperation operation) => operation.DescendantsAndSelf().Any(BlocksDeferral);

    /// <summary>
    /// Determines whether an expression has a type of its own. A throw expression and a target-typed <c>new()</c> have none,
    /// so a lambda whose body is one of them converts to every overload that takes a function, which the compiler reports as ambiguous.
    /// </summary>
    /// <param name="operation">The expression to test.</param>
    /// <returns><see langword="false"/> for a throw expression or a target-typed <c>new()</c>.</returns>
    internal static bool HasOwnType(IOperation operation) =>
        operation is not IThrowOperation and not IObjectCreationOperation { Syntax: ImplicitObjectCreationExpressionSyntax };

    /// <summary>
    /// Determines whether an expression has a type of its own. A <see langword="null"/> or <see langword="default"/> literal has none:
    /// as an argument it converts to a function as well as to the value, and as the result of a function it converts to a
    /// <see cref="System.Threading.Tasks.Task"/> as well as to the value, so a call of <c>ValueOr</c> that is given one binds to another overload.
    /// </summary>
    /// <param name="operation">The expression to test, a cheap one: a constant, a local, a parameter, a field or a literal.</param>
    /// <returns><see langword="false"/> for a <see langword="null"/> or <see langword="default"/> literal.</returns>
    internal static bool HasNaturalType(IOperation operation) =>
        operation.Syntax is not LiteralExpressionSyntax { RawKind: (int)SyntaxKind.NullLiteralExpression or (int)SyntaxKind.DefaultLiteralExpression };

    private static bool IsExpressionTree(ITypeSymbol type) =>
        type is INamedTypeSymbol named && named.ContainingNamespace.ToDisplayString() == "System.Linq.Expressions";

    private static bool BlocksDeferral(IOperation operation) => AwaitsOrPassesByReference(operation) || ReadsWhatALambdaCannotCapture(operation);

    private static bool AwaitsOrPassesByReference(IOperation operation) =>
        operation is IAwaitOperation or IDeclarationPatternOperation or IArgumentOperation { Parameter.RefKind: RefKind.Ref or RefKind.Out };

    private static bool ReadsWhatALambdaCannotCapture(IOperation operation) => operation switch
    {
        IParameterReferenceOperation reference => CannotCapture(reference.Parameter.RefKind, reference.Parameter.Type),
        ILocalReferenceOperation reference => CannotCapture(reference.Local.RefKind, reference.Local.Type),
        IInstanceReferenceOperation instance => instance.Type!.IsValueType,
        _ => false,
    };

    // A ref, out or in parameter, a ref local and a value of a ref struct cannot be captured by a lambda.
    private static bool CannotCapture(RefKind kind, ITypeSymbol type) => kind != RefKind.None || type.IsRefLikeType;
}
