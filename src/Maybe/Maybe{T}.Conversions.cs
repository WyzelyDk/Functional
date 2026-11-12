namespace Wyzely.Functional;

/// <content>
/// Conversions to and from other shapes: a plain value becomes a maybe implicitly (a <see langword="null"/> reference becomes none),
/// a maybe can be read as a sequence of zero or one values, and a maybe of a base type can be narrowed to a maybe of a derived type.
/// </content>
public readonly partial struct Maybe<T>
{
    /// <summary>Converts a value to a maybe. A <see langword="null"/> reference becomes none.</summary>
    /// <param name="value">The value, or <see langword="null"/>.</param>
    /// <returns>A maybe that holds the value, or none when it is <see langword="null"/>.</returns>
    public static implicit operator Maybe<T>(T? value) => Of(value);

    /// <summary>Returns the maybe as a sequence of one value, or an empty sequence when it has none.</summary>
    /// <returns>A sequence with zero or one element.</returns>
    public IEnumerable<T> ToEnumerable() => IsSome ? [_value] : [];

    /// <summary>
    /// Narrows the maybe to a maybe of a type that its value can have: the value when it is a <typeparamref name="TResult"/>, otherwise none.
    /// It is the maybe counterpart of <c>Enumerable.OfType</c>: a value of another type is dropped, not an error.
    /// </summary>
    /// <typeparam name="TResult">The type to narrow the value to. A base type, an interface and a boxed value type all count as types of the value.</typeparam>
    /// <returns>A maybe that holds the value as a <typeparamref name="TResult"/>, or none when there is no value or it is of another type.</returns>
    public Maybe<TResult> OfType<TResult>()
        where TResult : notnull => IsSome && _value is TResult result ? new Maybe<TResult>(result) : default;
}
