using System.Diagnostics;

namespace Wyzely.Functional;

/// <summary>
/// A value of type <typeparamref name="T"/> that may be absent: a maybe either holds a value (some) or does not (none).
/// </summary>
/// <typeparam name="T">The type of the value. It must not be nullable, because absence is what <c>None</c> is for.</typeparam>
/// <remarks>
/// <para>
/// A maybe is a <see langword="readonly"/> struct: the value is stored inline next to a flag, so creating, passing and chaining
/// maybes allocates nothing. The <see langword="default"/> value is none, so an unassigned field, an array element or
/// <c>default(Maybe&lt;T&gt;)</c> is a valid "no value" and never an error.
/// </para>
/// <para>
/// Read it with <see cref="IsSome"/> and <see cref="IsNone"/>, or leave it with <c>Match</c>, <c>ValueOr</c> or
/// <see cref="TryGetValue(out T)"/>. <see cref="GetValue"/> is for the place where the state is known, and an analyzer (WYZ0201) reports
/// it anywhere else. A plain value converts to a maybe implicitly, and a <see langword="null"/> reference becomes none, so
/// <c>return user;</c> in a method that returns <c>Maybe&lt;User&gt;</c> does the right thing.
/// </para>
/// </remarks>
[DebuggerDisplay("{ToString(),nq}")]
public readonly partial struct Maybe<T> : IEquatable<Maybe<T>>, IComparable<Maybe<T>>
    where T : notnull
{
    /// <summary>The value of a maybe that has one, or <see langword="default"/> for none.</summary>
    private readonly T _value;

    /// <summary>Initializes a new instance of the <see cref="Maybe{T}"/> struct that holds a value.</summary>
    /// <param name="value">The value. It must not be <see langword="null"/>; the callers check.</param>
    internal Maybe(T value)
    {
        _value = value;
        IsSome = true;
    }

    /// <summary>Gets a value indicating whether the maybe holds a value.</summary>
    public bool IsSome { get; }

    /// <summary>Gets a value indicating whether the maybe holds no value.</summary>
    public bool IsNone => !IsSome;

    /// <summary>Creates a maybe from a value that may be <see langword="null"/>: <see langword="null"/> becomes none.</summary>
    /// <param name="value">The value, or <see langword="null"/>.</param>
    /// <returns>A maybe that holds the value, or none when it is <see langword="null"/>.</returns>
    internal static Maybe<T> Of(T? value) => value is null ? default : new Maybe<T>(value);

    /// <summary>
    /// Returns whether another maybe is in the same state: both hold a value or both are none. The generated task methods use it for
    /// steps that never change a value, to hand back the task they received instead of allocating a new one.
    /// </summary>
    /// <param name="other">The maybe to compare with.</param>
    /// <returns><see langword="true"/> when both maybes are in the same state.</returns>
    internal bool HasSameStateAs(Maybe<T> other) => IsSome == other.IsSome;

    /// <summary>
    /// Splits the maybe into its state and its value. The railway methods use it instead of asking <see cref="IsSome"/> and then
    /// <see cref="GetValue"/>, which check twice.
    /// </summary>
    /// <param name="value">The value of a maybe that has one, or <see langword="default"/> for none.</param>
    /// <returns><see langword="true"/> when the maybe holds a value.</returns>
    internal bool Unpack(out T value)
    {
        value = _value;

        return IsSome;
    }
}
