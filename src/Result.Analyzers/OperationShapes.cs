using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;

namespace Wyzely.Functional.ResultAnalyzers;

/// <summary>Reads the shapes of calls and lambdas that the suggestion rules look for, so that each rule only says what it wants to see.</summary>
internal static class OperationShapes
{
    private const string LibraryNamespace = "Wyzely.Functional";

    /// <summary>Determines whether a method is an extension method of the library's <c>ResultExtensions</c> with the given name.</summary>
    /// <param name="method">The method to test.</param>
    /// <param name="name">The name of the method.</param>
    /// <returns><see langword="true"/> for the method of the library.</returns>
    internal static bool IsResultExtension(IMethodSymbol method, string name) =>
        method.Name == name
        && method.ContainingType.Name == "ResultExtensions"
        && method.ContainingNamespace.ToDisplayString() == LibraryNamespace;

    /// <summary>Determines whether a method is a member of <c>Error</c>, <c>Result</c>, <c>Result&lt;T&gt;</c> or <c>ResultExtensions</c>.</summary>
    /// <param name="method">The method to test.</param>
    /// <returns><see langword="true"/> for a method of the library that creates or passes on results and errors.</returns>
    internal static bool IsLibraryMember(IMethodSymbol method) =>
        method.ContainingType.Name is "Error" or "Result" or "ResultExtensions"
        && method.ContainingNamespace.ToDisplayString() == LibraryNamespace;

    /// <summary>Determines whether a method is a static member of <c>Result</c> (the type without a value) with the given name.</summary>
    /// <param name="method">The method to test.</param>
    /// <param name="name">The name of the method.</param>
    /// <returns><see langword="true"/> for the factory of the library.</returns>
    internal static bool IsResultFactory(IMethodSymbol method, string name) =>
        method.Name == name
        && method.IsStatic
        && method.ContainingType is { Name: "Result", IsGenericType: false }
        && method.ContainingNamespace.ToDisplayString() == LibraryNamespace;

    /// <summary>Finds the argument that is bound to the parameter with the given name.</summary>
    /// <param name="invocation">The call.</param>
    /// <param name="parameterName">The name of the parameter.</param>
    /// <returns>The value of the argument, without conversions, or <see langword="null"/> when the call has no such argument.</returns>
    internal static IOperation? ArgumentOf(IInvocationOperation invocation, string parameterName)
    {
        foreach (var argument in invocation.Arguments)
        {
            // Every argument of a call of the library is bound to a parameter, so the parameter is never missing.
            if (argument.Parameter!.Name == parameterName)
            {
                return OperationHelpers.Unwrap(argument.Value);
            }
        }

        return null;
    }

    /// <summary>Reads a lambda that has the given number of parameters and returns one expression, written as an expression or as a single return.</summary>
    /// <param name="operation">The operation that is expected to be a lambda converted to a delegate.</param>
    /// <param name="parameterCount">The number of parameters the lambda must have.</param>
    /// <param name="lambda">The lambda.</param>
    /// <param name="returned">The expression that the lambda returns, without conversions.</param>
    /// <returns><see langword="true"/> when the operation is such a lambda.</returns>
    internal static bool TryGetLambda(IOperation? operation, int parameterCount, out IAnonymousFunctionOperation lambda, out IOperation returned) =>
        TryGetLambda(operation, out lambda, out returned) && lambda.Symbol.Parameters.Length == parameterCount;

    /// <summary>Reads a lambda that returns one expression, written as an expression or as a single return, whatever its parameters are.</summary>
    /// <param name="operation">The operation that is expected to be a lambda converted to a delegate.</param>
    /// <param name="lambda">The lambda.</param>
    /// <param name="returned">The expression that the lambda returns, without conversions.</param>
    /// <returns><see langword="true"/> when the operation is such a lambda.</returns>
    internal static bool TryGetLambda(IOperation? operation, out IAnonymousFunctionOperation lambda, out IOperation returned)
    {
        lambda = null!;
        returned = null!;

        if (operation is not null
            && OperationHelpers.Unwrap(operation) is IDelegateCreationOperation { Target: IAnonymousFunctionOperation function }
            && function.Body.Operations.Length == 1
            && function.Body.Operations[0] is IReturnOperation { ReturnedValue: { } value })
        {
            lambda = function;
            returned = OperationHelpers.Unwrap(value);

            return true;
        }

        return false;
    }

    /// <summary>Determines whether an operation reads a parameter.</summary>
    /// <param name="operation">The operation to test.</param>
    /// <param name="parameter">The parameter.</param>
    /// <returns><see langword="true"/> when the operation is a plain read of the parameter.</returns>
    internal static bool IsParameter(IOperation operation, IParameterSymbol parameter) =>
        OperationHelpers.Unwrap(operation) is IParameterReferenceOperation reference && SymbolEqualityComparer.Default.Equals(reference.Parameter, parameter);

    /// <summary>Determines whether an operation reads a parameter anywhere inside it.</summary>
    /// <param name="operation">The operation to search.</param>
    /// <param name="parameter">The parameter.</param>
    /// <returns><see langword="true"/> when the parameter is read somewhere in the operation.</returns>
    internal static bool UsesParameter(IOperation operation, IParameterSymbol parameter)
    {
        if (operation is IParameterReferenceOperation reference && SymbolEqualityComparer.Default.Equals(reference.Parameter, parameter))
        {
            return true;
        }

        return operation.ChildOperations.Any(child => UsesParameter(child, parameter));
    }

    /// <summary>Determines whether an operation, or an operation inside it, is of a kind.</summary>
    /// <typeparam name="TOperation">The kind of operation to look for.</typeparam>
    /// <param name="operation">The operation to search.</param>
    /// <returns><see langword="true"/> when one is found, also inside a nested lambda.</returns>
    internal static bool Contains<TOperation>(IOperation operation)
        where TOperation : IOperation =>
        operation is TOperation || operation.ChildOperations.Any(Contains<TOperation>);

    /// <summary>
    /// Determines whether an operation assigns to a parameter, changes it with <c>++</c> or <c>--</c>,
    /// or passes it by <see langword="ref"/> or <see langword="out"/>, so that the value afterwards is not the value that came in.
    /// </summary>
    /// <param name="operation">The operation to search.</param>
    /// <param name="parameter">The parameter.</param>
    /// <returns><see langword="true"/> when the parameter may be changed.</returns>
    internal static bool ChangesParameter(IOperation operation, IParameterSymbol parameter)
    {
        var target = operation switch
        {
            IAssignmentOperation assignment => assignment.Target,
            IIncrementOrDecrementOperation increment => increment.Target,
            IArgumentOperation { Parameter.RefKind: RefKind.Ref or RefKind.Out } argument => argument.Value,
            _ => null,
        };

        return (target is not null && IsParameter(target, parameter)) || operation.ChildOperations.Any(child => ChangesParameter(child, parameter));
    }

    /// <summary>Determines whether an operation is a call of <c>Result.Success(x)</c> for the given parameter <c>x</c>.</summary>
    /// <param name="operation">The operation to test.</param>
    /// <param name="parameter">The parameter.</param>
    /// <returns><see langword="true"/> when the operation just wraps the parameter in a successful result.</returns>
    internal static bool IsSuccessOf(IOperation operation, IParameterSymbol parameter) =>
        OperationHelpers.Unwrap(operation) is IInvocationOperation { Arguments.Length: 1 } invocation
        && IsResultFactory(invocation.TargetMethod, "Success")
        && IsParameter(invocation.Arguments[0].Value, parameter);

    /// <summary>Determines whether a type is <c>Error</c> of the library.</summary>
    /// <param name="type">The type to test.</param>
    /// <returns><see langword="true"/> for <c>Error</c>.</returns>
    internal static bool IsError(ITypeSymbol? type) =>
        type is { Name: "Error" } && type.ContainingNamespace.ToDisplayString() == LibraryNamespace;

    /// <summary>Determines whether a type is <c>Result&lt;T&gt;</c> of the library.</summary>
    /// <param name="type">The type to test.</param>
    /// <returns><see langword="true"/> for a result with a value.</returns>
    internal static bool IsResultOfValue(ITypeSymbol? type) =>
        type is INamedTypeSymbol { Name: "Result", IsGenericType: true } named && named.ContainingNamespace.ToDisplayString() == LibraryNamespace;

    /// <summary>
    /// Determines whether a lambda can be converted to a type, so that a call that is given a lambda for a parameter of that type may bind to a
    /// value parameter instead of a function parameter: <c>object</c>, <c>dynamic</c>, <c>Delegate</c>, <c>MulticastDelegate</c>, a delegate type
    /// and an expression tree.
    /// </summary>
    /// <param name="type">The type of the value.</param>
    /// <returns><see langword="true"/> when a lambda would be taken for a value of the type.</returns>
    internal static bool LambdaConvertsTo(ITypeSymbol type) =>
        type.TypeKind is TypeKind.Delegate or TypeKind.Dynamic
        || type.SpecialType is SpecialType.System_Object or SpecialType.System_Delegate or SpecialType.System_MulticastDelegate
        || IsExpressionTree(type);

    /// <summary>Determines whether an operation reads the error of a result by calling <c>GetError()</c> on the receiver, anywhere inside it.</summary>
    /// <param name="operation">The operation to search.</param>
    /// <param name="receiver">The receiver that the result is read from.</param>
    /// <returns><see langword="true"/> when <c>receiver.GetError()</c> is called inside the operation.</returns>
    internal static bool ReadsErrorOf(IOperation operation, IOperation receiver)
    {
        if (operation is IInvocationOperation { TargetMethod: { Name: "GetError", Parameters.Length: 0 }, Instance: { } instance }
            && OperationHelpers.SameText(instance.Syntax, receiver.Syntax))
        {
            return true;
        }

        return operation.ChildOperations.Any(child => ReadsErrorOf(child, receiver));
    }

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
