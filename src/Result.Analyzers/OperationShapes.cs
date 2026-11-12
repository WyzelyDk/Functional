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

        return (target is not null && OperationHelpers.IsParameter(target, parameter))
            || operation.ChildOperations.Any(child => ChangesParameter(child, parameter));
    }

    /// <summary>Determines whether an operation is a call of <c>Result.Success(x)</c> for the given parameter <c>x</c>.</summary>
    /// <param name="operation">The operation to test.</param>
    /// <param name="parameter">The parameter.</param>
    /// <returns><see langword="true"/> when the operation just wraps the parameter in a successful result.</returns>
    internal static bool IsSuccessOf(IOperation operation, IParameterSymbol parameter) =>
        OperationHelpers.Unwrap(operation) is IInvocationOperation { Arguments.Length: 1 } invocation
        && IsResultFactory(invocation.TargetMethod, "Success")
        && OperationHelpers.IsParameter(invocation.Arguments[0].Value, parameter);

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
}
