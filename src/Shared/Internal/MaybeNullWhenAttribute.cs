#if NETSTANDARD2_0

// This attribute exists in the BCL from .NET Core 3.0. On netstandard2.0 it is declared here (internal) so the compiler
// still understands the nullability contracts of the public API. Consumers get the contracts from the metadata.
namespace System.Diagnostics.CodeAnalysis;

/// <summary>Specifies that when a method returns the given value, the argument may be <see langword="null"/> even if the type does not allow it.</summary>
/// <param name="returnValue">The return value that triggers the guarantee.</param>
[AttributeUsage(AttributeTargets.Parameter, Inherited = false)]
internal sealed class MaybeNullWhenAttribute(bool returnValue) : Attribute
{
    /// <summary>
    /// Gets a value indicating whether the method must return <see langword="true"/> (rather than <see langword="false"/>) for the
    /// guarantee to hold.
    /// </summary>
    public bool ReturnValue { get; } = returnValue;
}

#endif
