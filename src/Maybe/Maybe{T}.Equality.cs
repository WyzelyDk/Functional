namespace Wyzely.Functional;

/// <content>
/// Value equality: two somes are equal when their values are, and two nones are always equal.
/// </content>
public readonly partial struct Maybe<T>
{
    private const int SomeHashSeed = 1;

    /// <summary>Determines whether two maybes are equal.</summary>
    /// <param name="left">The first maybe.</param>
    /// <param name="right">The second maybe.</param>
    /// <returns><see langword="true"/> when both are none, or both hold equal values.</returns>
    public static bool operator ==(Maybe<T> left, Maybe<T> right) => left.Equals(right);

    /// <summary>Determines whether two maybes are different.</summary>
    /// <param name="left">The first maybe.</param>
    /// <param name="right">The second maybe.</param>
    /// <returns><see langword="true"/> when one has a value and the other does not, or the values differ.</returns>
    public static bool operator !=(Maybe<T> left, Maybe<T> right) => !left.Equals(right);

    /// <inheritdoc />
    public bool Equals(Maybe<T> other)
    {
        if (IsSome != other.IsSome)
        {
            return false;
        }

        return !IsSome || EqualityComparer<T>.Default.Equals(_value, other._value);
    }

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is Maybe<T> other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() =>
        IsSome ? HashCodeHelper.Combine(SomeHashSeed, EqualityComparer<T>.Default.GetHashCode(_value)) : 0;
}
