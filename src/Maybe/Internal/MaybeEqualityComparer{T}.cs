namespace Wyzely.Functional.Internal;

/// <summary>Compares maybes by the state and, for two maybes that have a value, by the given comparer of the values.</summary>
/// <typeparam name="T">The type of the values.</typeparam>
/// <param name="valueComparer">Compares and hashes the values.</param>
internal sealed class MaybeEqualityComparer<T>(IEqualityComparer<T> valueComparer) : IEqualityComparer<Maybe<T>>
    where T : notnull
{
    /// <summary>Determines whether two maybes are equal under the comparer of the values.</summary>
    /// <param name="x">The first maybe.</param>
    /// <param name="y">The second maybe.</param>
    /// <returns><see langword="true"/> when both have no value, or both have a value that the comparer finds equal.</returns>
    public bool Equals(Maybe<T> x, Maybe<T> y) => x.Equals(y, valueComparer);

    /// <summary>Gets the hash code of a maybe under the comparer of the values.</summary>
    /// <param name="obj">The maybe.</param>
    /// <returns>The hash code.</returns>
    public int GetHashCode(Maybe<T> obj) => obj.GetHashCode(valueComparer);
}
