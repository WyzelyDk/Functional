using System.Diagnostics;

namespace Wyzely.Functional;

/// <summary>
/// The outcome of an operation that returns no value: it either succeeded or failed with an <see cref="Error"/>.
/// </summary>
/// <remarks>
/// <para>
/// A result is a <see langword="readonly"/> struct that holds one reference, so a successful result allocates nothing and
/// a failed one carries a single immutable <see cref="Error"/> (which can itself aggregate several failures).
/// </para>
/// <para>
/// The <see langword="default"/> value is a success: <c>default(Result)</c> is the same as <see cref="Success()"/>, so a field or an
/// array element that was never assigned holds a success. Create a failure with <see cref="Failure(Error)"/>.
/// </para>
/// <para>
/// Read the outcome with <see cref="IsSuccess"/> and <see cref="IsFailure"/>; <see cref="GetError"/> is for the place where
/// the result is known to have failed, and an analyzer (WYZ0102) reports it anywhere else.
/// Use <see cref="Result{T}"/> when the operation also produces a value.
/// </para>
/// </remarks>
[DebuggerDisplay("{ToString(),nq}")]
public readonly partial struct Result : IEquatable<Result>
{
    /// <summary>The error of a failure, or <see langword="null"/> for a success, which is also what a <see langword="default"/> result holds.</summary>
    private readonly Error? _error;

    /// <summary>Initializes a new instance of the <see cref="Result"/> struct as a failure.</summary>
    /// <param name="error">The error of the failure.</param>
    internal Result(Error error) => _error = error;

    /// <summary>Gets a value indicating whether the operation succeeded.</summary>
    public bool IsSuccess => _error is null;

    /// <summary>Gets a value indicating whether the operation failed.</summary>
    public bool IsFailure => _error is not null;

    /// <summary>
    /// Returns whether another result is in the same state: both are a success, or both hold the same error. The generated task
    /// methods use it for steps that never change a value, to hand back the task they received instead of allocating a new one.
    /// </summary>
    /// <param name="other">The result to compare with.</param>
    /// <returns><see langword="true"/> when both results are in the same state.</returns>
    internal bool HasSameStateAs(Result other) => ReferenceEquals(_error, other._error);

    /// <summary>
    /// Returns the error of a failed result, or <see langword="null"/> for a success. The railway methods use it instead
    /// of asking <see cref="IsFailure"/> and then <see cref="GetError"/>.
    /// </summary>
    /// <returns>The error of a failed result, or <see langword="null"/> when the result succeeded.</returns>
    internal Error? FailureOrNull() => _error;
}
