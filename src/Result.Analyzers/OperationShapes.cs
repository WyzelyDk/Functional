using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;

namespace Wyzely.Functional.ResultAnalyzers;

/// <summary>Reads the shapes of calls and lambdas that the suggestion rules look for, so that each rule only says what it wants to see.</summary>
internal static class OperationShapes
{
    /// <summary>Strips the implicit conversions and the parentheses around an operation.</summary>
    /// <param name="operation">The operation to unwrap.</param>
    /// <returns>The operation inside.</returns>
    internal static IOperation Unwrap(IOperation operation)
    {
        while (true)
        {
            switch (operation)
            {
                case IConversionOperation { IsImplicit: true } conversion:
                    operation = conversion.Operand;
                    break;
                case IParenthesizedOperation parenthesized:
                    operation = parenthesized.Operand;
                    break;
                default:
                    return operation;
            }
        }
    }

    /// <summary>Determines whether a method is an extension method of the library's <c>ResultExtensions</c> with the given name.</summary>
    /// <param name="method">The method to test.</param>
    /// <param name="name">The name of the method.</param>
    /// <returns><see langword="true"/> for the method of the library.</returns>
    internal static bool IsResultExtension(IMethodSymbol method, string name) =>
        method.Name == name
        && method.ContainingType.Name == "ResultExtensions"
        && method.ContainingNamespace.ToDisplayString() == "Wyzely.Functional";

    /// <summary>Determines whether a method is a member of <c>Error</c>, <c>Result</c>, <c>Result&lt;T&gt;</c> or <c>ResultExtensions</c>.</summary>
    /// <param name="method">The method to test.</param>
    /// <returns><see langword="true"/> for a method of the library that creates or passes on results and errors.</returns>
    internal static bool IsLibraryMember(IMethodSymbol method) =>
        method.ContainingType.Name is "Error" or "Result" or "ResultExtensions"
        && method.ContainingNamespace.ToDisplayString() == "Wyzely.Functional";

    /// <summary>Determines whether a method is a static member of <c>Result</c> (the type without a value) with the given name.</summary>
    /// <param name="method">The method to test.</param>
    /// <param name="name">The name of the method.</param>
    /// <returns><see langword="true"/> for the factory of the library.</returns>
    internal static bool IsResultFactory(IMethodSymbol method, string name) =>
        method.Name == name
        && method.IsStatic
        && method.ContainingType is { Name: "Result", IsGenericType: false }
        && method.ContainingNamespace.ToDisplayString() == "Wyzely.Functional";

    /// <summary>Finds the argument that is bound to the parameter with the given name.</summary>
    /// <param name="invocation">The call.</param>
    /// <param name="parameterName">The name of the parameter.</param>
    /// <returns>The value of the argument, without conversions, or <see langword="null"/> when the call has no such argument.</returns>
    internal static IOperation? ArgumentOf(IInvocationOperation invocation, string parameterName)
    {
        foreach (var argument in invocation.Arguments)
        {
            if (argument.Parameter?.Name == parameterName)
            {
                return Unwrap(argument.Value);
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
    internal static bool TryGetLambda(IOperation? operation, int parameterCount, out IAnonymousFunctionOperation lambda, out IOperation returned)
    {
        lambda = null!;
        returned = null!;

        if (operation is not null
            && Unwrap(operation) is IDelegateCreationOperation { Target: IAnonymousFunctionOperation function }
            && function.Symbol.Parameters.Length == parameterCount
            && function.Body.Operations.Length == 1
            && function.Body.Operations[0] is IReturnOperation { ReturnedValue: { } value })
        {
            lambda = function;
            returned = Unwrap(value);

            return true;
        }

        return false;
    }

    /// <summary>Determines whether an operation reads a parameter.</summary>
    /// <param name="operation">The operation to test.</param>
    /// <param name="parameter">The parameter.</param>
    /// <returns><see langword="true"/> when the operation is a plain read of the parameter.</returns>
    internal static bool IsParameter(IOperation operation, IParameterSymbol parameter) =>
        Unwrap(operation) is IParameterReferenceOperation reference && SymbolEqualityComparer.Default.Equals(reference.Parameter, parameter);

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

    /// <summary>
    /// Determines whether evaluating an operation ahead of time costs nothing and cannot change anything: a constant, or a read of a local, a
    /// parameter or a field. Anything else (a call, an allocation) has to stay lazy when a rewrite would otherwise evaluate it on every call.
    /// </summary>
    /// <param name="operation">The operation to test.</param>
    /// <returns><see langword="true"/> for a cheap and safe operation.</returns>
    internal static bool IsCheap(IOperation operation)
    {
        var inner = Unwrap(operation);

        return inner.ConstantValue.HasValue
            || inner is ILocalReferenceOperation or IParameterReferenceOperation or IFieldReferenceOperation or IDefaultValueOperation;
    }

    /// <summary>Determines whether an operation, or an operation inside it, is of a kind.</summary>
    /// <typeparam name="TOperation">The kind of operation to look for.</typeparam>
    /// <param name="operation">The operation to search.</param>
    /// <returns><see langword="true"/> when one is found, also inside a nested lambda.</returns>
    internal static bool Contains<TOperation>(IOperation operation)
        where TOperation : IOperation =>
        operation is TOperation || operation.ChildOperations.Any(Contains<TOperation>);

    /// <summary>
    /// Determines whether an operation assigns to a parameter, changes it with <c>++</c> or <c>--</c>, or passes it by <see langword="ref"/> or
    /// <see langword="out"/>, so that the value afterwards is not the value that came in.
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
        Unwrap(operation) is IInvocationOperation { Arguments.Length: 1 } invocation
        && IsResultFactory(invocation.TargetMethod, "Success")
        && IsParameter(invocation.Arguments[0].Value, parameter);
}
