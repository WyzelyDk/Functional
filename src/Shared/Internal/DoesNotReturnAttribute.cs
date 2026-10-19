#if NETSTANDARD2_0

// This attribute exists in the BCL from .NET Core 3.0. On netstandard2.0 it is declared here (internal) so the compiler
// still understands the nullability contracts of the public API. Consumers get the contracts from the metadata.
namespace System.Diagnostics.CodeAnalysis;

/// <summary>Specifies that a method never returns under any circumstance.</summary>
[AttributeUsage(AttributeTargets.Method, Inherited = false)]
internal sealed class DoesNotReturnAttribute : Attribute
{
}

#endif
