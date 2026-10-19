#if NET11_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Wyzely.Functional;

/// <content>
/// C# 15 union support. A <see cref="Maybe{T}"/> is a union of its one case type, <typeparamref name="T"/>, and the absence of a value,
/// which a pattern sees as <see langword="null"/>, so that a <see langword="switch"/> can match on both and the compiler can check
/// that both are handled: <c>maybe switch { int value =&gt; ..., null =&gt; ... }</c>.
/// This is the union member provider form of a custom union type, because the creation of a maybe goes through the factory
/// methods rather than through public constructors; it adds no members to the type apart from the nested
/// <see cref="IUnionMembers"/> interface that the compiler reads. The <see langword="default"/> maybe is none, so a
/// <see langword="switch"/> over it matches the <see langword="null"/> case.
/// </content>
[Union]
public readonly partial struct Maybe<T> : Maybe<T>.IUnionMembers
{
    /// <summary>
    /// The members that the C# compiler uses to treat <see cref="Maybe{T}"/> as a union. They are not meant to be called
    /// directly: use pattern matching.
    /// </summary>
    [SuppressMessage(
        "Design",
        "CA1000:Do not declare static members on generic types",
        Justification = "The union member provider pattern of C# 15 requires static Create methods on this interface.")]
    public interface IUnionMembers
    {
        /// <summary>Gets the contained case: the value of a some, or <see langword="null"/> for none.</summary>
        object? Value { get; }

        /// <summary>Gets a value indicating whether <see cref="Value"/> is not <see langword="null"/>.</summary>
        bool HasValue { get; }

        /// <summary>Creates a maybe from a value. A <see langword="null"/> reference becomes none.</summary>
        /// <param name="value">The value.</param>
        /// <returns>A maybe that holds the value, or none when it is <see langword="null"/>.</returns>
        static Maybe<T> Create(T value) => Of(value);

        /// <summary>Gets the value without boxing.</summary>
        /// <param name="value">The value of a some.</param>
        /// <returns><see langword="true"/> for a some.</returns>
        bool TryGetValue(out T value);
    }

    /// <inheritdoc />
    object? IUnionMembers.Value => IsSome ? _value : null;

    /// <inheritdoc />
    bool IUnionMembers.HasValue => IsSome;

    /// <inheritdoc />
    bool IUnionMembers.TryGetValue(out T value)
    {
        value = _value;

        return IsSome;
    }
}
#endif
