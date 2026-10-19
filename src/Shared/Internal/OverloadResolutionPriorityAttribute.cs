#if !NET9_0_OR_GREATER

// This attribute exists in the BCL from .NET 9. On earlier targets it is declared here (internal): the C# 13 compiler
// reads it from the metadata of this assembly by name, so consumers still get the overload priorities.
namespace System.Runtime.CompilerServices;

/// <summary>
/// Specifies the priority of a member in overload resolution. Among the applicable candidates, those with the highest priority
/// win. Here it makes the <see cref="Task"/> overloads win over the
/// <see cref="ValueTask"/> overloads when an <c>async</c> lambda fits both.
/// </summary>
/// <param name="priority">The priority of the member. Higher wins; the default priority is 0.</param>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor | AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
internal sealed class OverloadResolutionPriorityAttribute(int priority) : Attribute
{
    /// <summary>Gets the priority of the member.</summary>
    public int Priority { get; } = priority;
}

#endif
