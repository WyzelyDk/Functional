using System.Diagnostics.CodeAnalysis;

namespace Wyzely.Functional;

/// <content>
/// Reading the value of a maybe, with and without a throwing accessor.
/// </content>
public readonly partial struct Maybe<T>
{
    /// <summary>Returns the value of a maybe that has one. Check <see cref="IsSome"/> first.</summary>
    /// <returns>The value of the maybe.</returns>
    /// <exception cref="InvalidOperationException">
    /// The maybe has no value. The message can be changed with <see cref="MaybeOptions.NoValueMessage"/>.
    /// </exception>
    public T GetValue()
    {
        if (!IsSome)
        {
            MaybeThrow.NoValue();
        }

        return _value;
    }

    /// <summary>Gets the value of a maybe without throwing when it has none.</summary>
    /// <param name="value">The value of the maybe, or <see langword="default"/> when it has none.</param>
    /// <returns><see langword="true"/> when the maybe holds a value.</returns>
    public bool TryGetValue([MaybeNullWhen(false)] out T value)
    {
        value = _value;

        return IsSome;
    }

    /// <summary>Returns the value of a maybe that has one, or a fallback when it has none.</summary>
    /// <param name="fallback">The value to return when the maybe has none.</param>
    /// <returns>The value, or <paramref name="fallback"/>.</returns>
    public T ValueOr(T fallback) => IsSome ? _value : fallback;
}
