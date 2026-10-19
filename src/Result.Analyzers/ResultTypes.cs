using Microsoft.CodeAnalysis;

namespace Wyzely.Functional.ResultAnalyzers;

/// <summary>The symbols the analyzer needs, resolved once per compilation.</summary>
internal sealed class ResultTypes
{
    private readonly INamedTypeSymbol _result;
    private readonly INamedTypeSymbol _resultOfT;
    private readonly INamedTypeSymbol? _taskOfT;
    private readonly INamedTypeSymbol? _valueTaskOfT;

    private ResultTypes(INamedTypeSymbol result, INamedTypeSymbol resultOfT, INamedTypeSymbol? taskOfT, INamedTypeSymbol? valueTaskOfT)
    {
        _result = result;
        _resultOfT = resultOfT;
        _taskOfT = taskOfT;
        _valueTaskOfT = valueTaskOfT;
    }

    /// <summary>Resolves the symbols of a compilation, or returns <see langword="null"/> when the compilation does not know the result types.</summary>
    /// <param name="compilation">The compilation to look in.</param>
    /// <returns>The resolved symbols, or <see langword="null"/>.</returns>
    internal static ResultTypes? Create(Compilation compilation)
    {
        var result = compilation.GetTypeByMetadataName("Wyzely.Functional.Result");
        var resultOfT = compilation.GetTypeByMetadataName("Wyzely.Functional.Result`1");

        if (result is null || resultOfT is null)
        {
            return null;
        }

        return new ResultTypes(
            result,
            resultOfT,
            compilation.GetTypeByMetadataName("System.Threading.Tasks.Task`1"),
            compilation.GetTypeByMetadataName("System.Threading.Tasks.ValueTask`1"));
    }

    /// <summary>Determines whether a type is <c>Result</c> or <c>Result&lt;T&gt;</c>.</summary>
    /// <param name="type">The type to test.</param>
    /// <returns><see langword="true"/> for a result type.</returns>
    internal bool IsResult(ITypeSymbol? type) =>
        type is INamedTypeSymbol named
        && (SymbolEqualityComparer.Default.Equals(named, _result)
            || (named.IsGenericType && SymbolEqualityComparer.Default.Equals(named.OriginalDefinition, _resultOfT)));

    /// <summary>Determines whether a type is a <c>Task</c> or <c>ValueTask</c> of a result type.</summary>
    /// <param name="type">The type to test.</param>
    /// <returns><see langword="true"/> for an awaitable that produces a result.</returns>
    internal bool IsAwaitableResult(ITypeSymbol? type) =>
        type is INamedTypeSymbol { IsGenericType: true } named
        && (SymbolEqualityComparer.Default.Equals(named.OriginalDefinition, _taskOfT)
            || SymbolEqualityComparer.Default.Equals(named.OriginalDefinition, _valueTaskOfT))
        && IsResult(named.TypeArguments[0]);
}
