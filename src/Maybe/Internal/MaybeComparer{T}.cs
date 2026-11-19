namespace Wyzely.Functional.Internal;

/// <summary>Orders maybes: none first, then the values by the given comparer.</summary>
/// <typeparam name="T">The type of the values.</typeparam>
/// <param name="valueComparer">Compares the values.</param>
internal sealed class MaybeComparer<T>(IComparer<T> valueComparer) : IComparer<Maybe<T>>
    where T : notnull
{
    /// <summary>Compares two maybes.</summary>
    /// <param name="x">The first maybe.</param>
    /// <param name="y">The second maybe.</param>
    /// <returns>A negative number, zero or a positive number, as <paramref name="x"/> sorts before, with or after <paramref name="y"/>.</returns>
    public int Compare(Maybe<T> x, Maybe<T> y) => x.CompareTo(y, valueComparer);
}
