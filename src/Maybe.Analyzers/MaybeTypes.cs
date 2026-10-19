using Microsoft.CodeAnalysis;

namespace Wyzely.Functional.MaybeAnalyzers;

/// <summary>The symbols the analyzer needs, resolved once per compilation.</summary>
internal sealed class MaybeTypes
{
    private readonly INamedTypeSymbol _maybeOfT;

    private MaybeTypes(INamedTypeSymbol maybeOfT) => _maybeOfT = maybeOfT;

    /// <summary>Resolves the symbols of a compilation, or returns <see langword="null"/> when the compilation does not know the maybe type.</summary>
    /// <param name="compilation">The compilation to look in.</param>
    /// <returns>The resolved symbols, or <see langword="null"/>.</returns>
    internal static MaybeTypes? Create(Compilation compilation)
    {
        var maybeOfT = compilation.GetTypeByMetadataName("Wyzely.Functional.Maybe`1");

        return maybeOfT is null ? null : new MaybeTypes(maybeOfT);
    }

    /// <summary>Determines whether a type is <c>Maybe&lt;T&gt;</c>.</summary>
    /// <param name="type">The type to test.</param>
    /// <returns><see langword="true"/> for a maybe type.</returns>
    internal bool IsMaybe(ITypeSymbol? type) =>
        type is INamedTypeSymbol { IsGenericType: true } named
        && SymbolEqualityComparer.Default.Equals(named.OriginalDefinition, _maybeOfT);
}
