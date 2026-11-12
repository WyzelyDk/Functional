using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;

namespace Wyzely.Functional;

/// <content>Reading calls and the lambdas that are passed to them.</content>
internal static partial class OperationHelpers
{
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
            && Unwrap(operation) is IDelegateCreationOperation { Target: IAnonymousFunctionOperation function }
            && function.Body.Operations.Length == 1
            && function.Body.Operations[0] is IReturnOperation { ReturnedValue: { } value })
        {
            lambda = function;
            returned = Unwrap(value);

            return true;
        }

        return false;
    }

    /// <summary>Reads the lambda that is passed for a parameter when it is not async, has the given number of parameters and returns one expression.</summary>
    /// <param name="invocation">The call.</param>
    /// <param name="parameterName">The name of the parameter that takes the lambda.</param>
    /// <param name="parameterCount">The number of parameters the lambda must have.</param>
    /// <param name="lambda">The lambda.</param>
    /// <param name="returned">The expression that the lambda returns, without conversions.</param>
    /// <returns><see langword="true"/> when the argument is such a lambda.</returns>
    internal static bool TryGetSynchronousLambda(
        IInvocationOperation invocation,
        string parameterName,
        int parameterCount,
        out IAnonymousFunctionOperation lambda,
        out IOperation returned) =>
        TryGetLambda(ArgumentOf(invocation, parameterName), parameterCount, out lambda, out returned) && !lambda.Symbol.IsAsync;

    /// <summary>Determines whether an operation reads a parameter.</summary>
    /// <param name="operation">The operation to test.</param>
    /// <param name="parameter">The parameter.</param>
    /// <returns><see langword="true"/> when the operation is a plain read of the parameter.</returns>
    internal static bool IsParameter(IOperation operation, IParameterSymbol parameter) =>
        Unwrap(operation) is IParameterReferenceOperation reference && SymbolEqualityComparer.Default.Equals(reference.Parameter, parameter);

    /// <summary>
    /// Determines whether a call is written as <c>receiver.Name(...)</c> without type arguments, which is what a fix that renames the call needs.
    /// An extension method that is called as a static method, <c>Extensions.Name(receiver, ...)</c>, has the same syntax
    /// but one argument more than the call that a fix writes.
    /// </summary>
    /// <param name="invocation">The call.</param>
    /// <returns><see langword="true"/> for a call that can be renamed.</returns>
    internal static bool IsPlainMemberCall(IInvocationOperation invocation) =>
        invocation.Syntax is InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax { Name: IdentifierNameSyntax } } call
        && (!invocation.TargetMethod.IsExtensionMethod || call.ArgumentList.Arguments.Count == invocation.TargetMethod.Parameters.Length - 1);

    /// <summary>Determines whether a method has two type arguments that are the same type, like <c>Map&lt;int, int&gt;</c>.</summary>
    /// <param name="method">The method.</param>
    /// <returns><see langword="true"/> when there are two type arguments and they are equal.</returns>
    internal static bool HasEqualTypeArguments(IMethodSymbol method) =>
        method.TypeArguments.Length == 2 && SymbolEqualityComparer.Default.Equals(method.TypeArguments[0], method.TypeArguments[1]);

    /// <summary>
    /// Gets where a diagnostic about a call is reported: the name of the method for a call that is written as a member access,
    /// so that the squiggle is short, and the whole expression otherwise.
    /// </summary>
    /// <param name="operation">The operation the diagnostic is about.</param>
    /// <returns>The location.</returns>
    internal static Location LocationOf(IOperation operation) =>
        operation.Syntax is InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax access }
            ? access.Name.GetLocation()
            : operation.Syntax.GetLocation();

    /// <summary>Makes a property of a diagnostic that holds a flag, in the form the code fix reads.</summary>
    /// <param name="name">The name of the property.</param>
    /// <param name="value">The flag.</param>
    /// <returns>The property, with the value <c>true</c> or <c>false</c>.</returns>
    internal static KeyValuePair<string, string?> Flag(string name, bool value) => new(name, value ? "true" : "false");
}
