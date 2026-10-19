#if NET11_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Wyzely.Functional;

/// <content>
/// C# 15 union support. A <see cref="Result{T}"/> is a union of its two case types, <typeparamref name="T"/> (a success) and
/// <see cref="Error"/> (a failure), so that a <see langword="switch"/> can match on them and the compiler can check that both
/// are handled: <c>result switch { int value =&gt; ..., Error error =&gt; ... }</c>.
/// This is the union member provider form of a custom union type, because the creation of a result goes through the factory
/// methods rather than through public constructors; it adds no members to the type apart from the nested
/// <see cref="IUnionMembers"/> interface that the compiler reads. A <see langword="default"/> result is a success, so a
/// <see langword="switch"/> over it matches the value case. A <c>Result&lt;Error&gt;</c> cannot be used as a union, because both case types
/// are the same.
/// </content>
[Union]
public readonly partial struct Result<T> : Result<T>.IUnionMembers
{
    /// <summary>
    /// The members that the C# compiler uses to treat <see cref="Result{T}"/> as a union. They are not meant to be called
    /// directly: use pattern matching.
    /// </summary>
    [SuppressMessage(
        "Design",
        "CA1000:Do not declare static members on generic types",
        Justification = "The union member provider pattern of C# 15 requires static Create methods on this interface.")]
    public interface IUnionMembers
    {
        /// <summary>
        /// Gets the contained case: the value of a success, the <see cref="Error"/> of a failure, or <see langword="null"/> for a success
        /// whose value is <see langword="null"/>.
        /// </summary>
        object? Value { get; }

        /// <summary>Gets a value indicating whether <see cref="Value"/> is not <see langword="null"/>.</summary>
        bool HasValue { get; }

        /// <summary>Creates a successful result.</summary>
        /// <param name="value">The value of the success.</param>
        /// <returns>A successful result.</returns>
        static Result<T> Create(T value) => Result.Success(value);

        /// <summary>Creates a failed result.</summary>
        /// <param name="value">The error of the failure.</param>
        /// <returns>A failed result.</returns>
        static Result<T> Create(Error value) => Result.Failure<T>(value);

        /// <summary>Gets the value without boxing.</summary>
        /// <param name="value">The value of a successful result.</param>
        /// <returns><see langword="true"/> for a success whose value is not <see langword="null"/>.</returns>
        bool TryGetValue(out T value);

        /// <summary>Gets the error.</summary>
        /// <param name="value">The error of a failed result.</param>
        /// <returns><see langword="true"/> for a failure.</returns>
        bool TryGetValue(out Error value);
    }

    /// <inheritdoc />
    object? IUnionMembers.Value => _error is null ? _value : _error;

    /// <inheritdoc />
    bool IUnionMembers.HasValue => ((IUnionMembers)this).Value is not null;

    /// <inheritdoc />
    bool IUnionMembers.TryGetValue(out T value)
    {
        value = _value;

        return _error is null && _value is not null;
    }

    /// <inheritdoc />
    bool IUnionMembers.TryGetValue(out Error value)
    {
        value = _error!;

        return _error is not null;
    }
}
#endif
