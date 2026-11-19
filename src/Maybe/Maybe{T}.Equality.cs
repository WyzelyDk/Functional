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

    /// <summary>
    /// Determines whether this maybe equals another one, comparing the values with the given comparer instead of the default equality of the type.
    /// </summary>
    /// <param name="other">The maybe to compare with.</param>
    /// <param name="comparer">Compares the values when both maybes have one.</param>
    /// <returns><see langword="true"/> when both have no value, or both have a value that <paramref name="comparer"/> finds equal.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="comparer"/> is <see langword="null"/>.</exception>
    public bool Equals(Maybe<T> other, IEqualityComparer<T> comparer)
    {
        Guard.NotNull(comparer, nameof(comparer));

        if (IsSome != other.IsSome)
        {
            return false;
        }

        return !IsSome || comparer.Equals(_value, other._value);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        if (!IsSome)
        {
            return 0;
        }

        var valueHash = EqualityComparer<T>.Default.GetHashCode(_value);

        return HashCodeHelper.Combine(SomeHashSeed, valueHash);
    }

    /// <summary>
    /// Gets the hash code of this maybe, hashing the value with the given comparer. It matches <see cref="Equals(Maybe{T}, IEqualityComparer{T})"/>.
    /// </summary>
    /// <param name="comparer">Hashes the value when there is one.</param>
    /// <returns>A hash code that is the same for two maybes that <paramref name="comparer"/> finds equal.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="comparer"/> is <see langword="null"/>.</exception>
    public int GetHashCode(IEqualityComparer<T> comparer)
    {
        Guard.NotNull(comparer, nameof(comparer));

        if (!IsSome)
        {
            return 0;
        }

        var valueHash = comparer.GetHashCode(_value);

        return HashCodeHelper.Combine(SomeHashSeed, valueHash);
    }
}
