using System.Diagnostics.CodeAnalysis;

namespace Wyzely.Functional.Internal;

/// <summary>
/// Throw helpers for the result types. Keeping the <c>throw</c> statements out of the members that call them keeps those
/// members small enough for the JIT to inline them.
/// </summary>
internal static class ResultThrow
{
    /// <summary>Throws the exception for reading the error of a successful result.</summary>
    /// <exception cref="InvalidOperationException">Always thrown.</exception>
    [DoesNotReturn]
    internal static void NotFailed() =>
        throw new InvalidOperationException("The result succeeded and has no error. Check IsFailure first, or use TryGetError.");

    /// <summary>Throws the exception for reading the value of, or demanding success from, a failed result.</summary>
    /// <param name="error">The error of the failed result.</param>
    /// <exception cref="ResultFailedException">Always thrown.</exception>
    [DoesNotReturn]
    internal static void Failed(Error error) => throw new ResultFailedException(error);
}
