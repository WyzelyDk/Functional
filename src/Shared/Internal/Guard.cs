using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Wyzely.Functional.Internal;

/// <summary>
/// Argument validation helpers shared by the public API. They exist so that every public method validates its input
/// the same way and so that the library does not depend on <c>ArgumentNullException.ThrowIfNull</c>, which is missing on netstandard2.0.
/// </summary>
internal static class Guard
{
    /// <summary>Returns <paramref name="value"/> or throws when it is <see langword="null"/>.</summary>
    /// <typeparam name="T">The reference type of the argument.</typeparam>
    /// <param name="value">The argument to validate.</param>
    /// <param name="paramName">The name of the argument, reported by the exception.</param>
    /// <returns>The validated, non-null argument.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/>.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static T NotNull<T>([NotNull] T? value, string paramName)
        where T : class
    {
        if (value is null)
        {
            ThrowNull(paramName);
        }

        return value;
    }

    /// <summary>Returns <paramref name="value"/> or throws when it is <see langword="null"/>, empty or only white space.</summary>
    /// <param name="value">The text to validate.</param>
    /// <param name="paramName">The name of the argument, reported by the exception.</param>
    /// <returns>The validated text.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="value"/> is empty or consists only of white space.</exception>
    internal static string NotNullOrWhiteSpace([NotNull] string? value, string paramName)
    {
        if (value is null)
        {
            throw new ArgumentNullException(paramName);
        }

        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("The value cannot be empty or consist only of white space.", paramName);
        }

        return value;
    }

    // The throw lives in its own method so that NotNull stays small enough to be inlined into every caller.
    [DoesNotReturn]
    private static void ThrowNull(string paramName) => throw new ArgumentNullException(paramName);
}
