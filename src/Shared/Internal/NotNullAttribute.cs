#if NETSTANDARD2_0

// This attribute exists in the BCL from .NET Core 3.0. On netstandard2.0 it is declared here (internal) so the compiler
// still understands the nullability contracts of the public API. Consumers get the contracts from the metadata.
namespace System.Diagnostics.CodeAnalysis;

/// <summary>Specifies that an output is not <see langword="null"/> even if the corresponding type allows it.</summary>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Parameter | AttributeTargets.Property | AttributeTargets.ReturnValue, Inherited = false)]
internal sealed class NotNullAttribute : Attribute
{
}

#endif
