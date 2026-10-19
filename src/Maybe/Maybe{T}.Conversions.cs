namespace Wyzely.Functional;

/// <content>
/// Conversions to and from other shapes: a plain value becomes a maybe implicitly (a <see langword="null"/> reference becomes none),
/// and a maybe can be read as a sequence of zero or one values.
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
}
