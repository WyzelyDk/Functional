namespace Wyzely.Functional;

/// <content>
/// Ordering: none sorts before every some, and somes sort by their values.
/// </content>
public readonly partial struct Maybe<T>
{
    /// <summary>Determines whether the first maybe sorts before the second.</summary>
    /// <param name="left">The first maybe.</param>
    /// <param name="right">The second maybe.</param>
    /// <returns><see langword="true"/> when <paramref name="left"/> sorts before <paramref name="right"/>.</returns>
    public static bool operator <(Maybe<T> left, Maybe<T> right) => left.CompareTo(right) < 0;

    /// <summary>Determines whether the first maybe sorts before the second or equally.</summary>
    /// <param name="left">The first maybe.</param>
    /// <param name="right">The second maybe.</param>
    /// <returns><see langword="true"/> when <paramref name="left"/> does not sort after <paramref name="right"/>.</returns>
    public static bool operator <=(Maybe<T> left, Maybe<T> right) => left.CompareTo(right) <= 0;

    /// <summary>Determines whether the first maybe sorts after the second.</summary>
    /// <param name="left">The first maybe.</param>
    /// <param name="right">The second maybe.</param>
    /// <returns><see langword="true"/> when <paramref name="left"/> sorts after <paramref name="right"/>.</returns>
    public static bool operator >(Maybe<T> left, Maybe<T> right) => left.CompareTo(right) > 0;

    /// <summary>Determines whether the first maybe sorts after the second or equally.</summary>
    /// <param name="left">The first maybe.</param>
    /// <param name="right">The second maybe.</param>
    /// <returns><see langword="true"/> when <paramref name="left"/> does not sort before <paramref name="right"/>.</returns>
    public static bool operator >=(Maybe<T> left, Maybe<T> right) => left.CompareTo(right) >= 0;

    /// <summary>Compares this maybe with another one. None sorts before a some, and two somes are compared by their values.</summary>
    /// <param name="other">The maybe to compare with.</param>
    /// <returns>A negative number when this maybe sorts first, zero when they sort equally, and a positive number when it sorts last.</returns>
    public int CompareTo(Maybe<T> other)
    {
        if (!IsSome)
        {
            return other.IsSome ? -1 : 0;
        }

        return other.IsSome ? Comparer<T>.Default.Compare(_value, other._value) : 1;
    }
}
