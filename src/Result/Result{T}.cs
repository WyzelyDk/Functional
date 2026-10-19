using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Wyzely.Functional;

/// <summary>
/// The outcome of an operation that returns a value of type <typeparamref name="T"/>: it either succeeded with a value or
/// failed with an <see cref="Error"/>.
/// </summary>
/// <typeparam name="T">The type of the value of a successful result.</typeparam>
/// <remarks>
/// <para>
/// A result is a <see langword="readonly"/> struct. The value is stored inline (no boxing, no allocation) next to a single
/// reference that holds the error, and the value of a failed result is simply <see langword="default"/>.
/// </para>
/// <para>
/// The <see langword="default"/> value is a success that holds <c>default(T)</c>: <c>default(Result&lt;int&gt;)</c> is the same as
/// <c>Result.Success(0)</c>, so a field or an array element that was never assigned holds a success. A success may hold a
/// <see langword="null"/> value. Create a failure with <see cref="Result.Failure{T}(Error)"/>.
/// </para>
/// <para>
/// Read the outcome with <see cref="IsSuccess"/> and <see cref="IsFailure"/>; <see cref="GetValue"/> and
/// <see cref="GetError"/> are for the place where the state is known, and an analyzer (WYZ0102) reports them anywhere else.
/// Nothing converts to a result implicitly: create results with <see cref="Result.Success{T}(T)"/> and <see cref="Result.Failure{T}(Error)"/>,
/// so that <c>return false;</c> in a method that returns <c>Result&lt;bool&gt;</c> cannot be mistaken for a failure.
/// </para>
/// </remarks>
[DebuggerDisplay("{ToString(),nq}")]
public readonly partial struct Result<T> : IEquatable<Result<T>>
{
    /// <summary>The value of a success, or <see langword="default"/> for a failure.</summary>
    private readonly T _value;

    /// <summary>The error of a failure, or <see langword="null"/> for a success, which is also what a <see langword="default"/> result holds.</summary>
    private readonly Error? _error;

    /// <summary>Initializes a new instance of the <see cref="Result{T}"/> struct.</summary>
    /// <param name="value">The value of a success, or <see langword="default"/> for a failure.</param>
    /// <param name="error">The error of a failure, or <see langword="null"/> for a success.</param>
    private Result(T value, Error? error)
    {
        _value = value;
        _error = error;
    }

    /// <summary>Gets a value indicating whether the operation succeeded.</summary>
    public bool IsSuccess => _error is null;

    /// <summary>Gets a value indicating whether the operation failed.</summary>
    public bool IsFailure => _error is not null;

    /// <summary>Creates a successful result.</summary>
    /// <param name="value">The value.</param>
    /// <returns>A success that holds the value.</returns>
    /// <remarks>
    /// Forced to inline: without it the JIT stops inlining the railway methods that call it into their callers, and a step of a chain on a
    /// <see cref="ValueTask{TResult}"/> costs several nanoseconds more.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static Result<T> Succeeded(T value) => new(value, null);

    /// <summary>Creates a failed result.</summary>
    /// <param name="error">The error of the failure.</param>
    /// <returns>A failure with the error and no value.</returns>
    /// <remarks>Forced to inline, for the same reason as <see cref="Succeeded"/>.</remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static Result<T> Failed(Error error) => new(default!, error);

    /// <summary>
    /// Returns whether another result is in the same state: both are a success, or both hold the same error. The generated task
    /// methods use it for steps that never change a value, to hand back the task they received instead of allocating a new one.
    /// </summary>
    /// <param name="other">The result to compare with.</param>
    /// <returns><see langword="true"/> when both results are in the same state.</returns>
    internal bool HasSameStateAs(Result<T> other) => ReferenceEquals(_error, other._error);

    /// <summary>
    /// Splits the result into its value and its error. The railway methods use it instead of asking <see cref="IsFailure"/> and
    /// then <see cref="GetValue"/> or <see cref="GetError"/>.
    /// </summary>
    /// <param name="value">The value of a successful result, or <see langword="default"/> when the result failed.</param>
    /// <returns>The error of a failed result, or <see langword="null"/> when the result succeeded.</returns>
    internal Error? Unpack(out T value)
    {
        value = _value;

        return _error;
    }
}
