using System.Diagnostics.CodeAnalysis;

namespace Wyzely.Functional.Internal;

/// <summary>
/// Throw helpers for the maybe type. Keeping the <c>throw</c> statements out of the members that call them keeps those
/// members small enough for the JIT to inline them.
/// </summary>
internal static class MaybeThrow
{
    /// <summary>Throws the exception for reading the value of a maybe that has none.</summary>
    /// <exception cref="InvalidOperationException">Always thrown.</exception>
    [DoesNotReturn]
    internal static void NoValue() => throw new InvalidOperationException(MaybeConfiguration.Current.NoValueMessage);

    /// <summary>Throws the exception for creating a some from <see langword="null"/>.</summary>
    /// <param name="paramName">The name of the argument.</param>
    /// <exception cref="ArgumentNullException">Always thrown.</exception>
    [DoesNotReturn]
    internal static void NullValue(string paramName) =>
        throw new ArgumentNullException(paramName, "A some needs a value. Use Maybe.From for a value that may be null, or Maybe.None for no value.");
}
