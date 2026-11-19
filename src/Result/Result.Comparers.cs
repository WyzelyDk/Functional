namespace Wyzely.Functional;

/// <content>
/// The comparer for collections of results, which takes the comparer of the values.
/// </content>
public readonly partial struct Result
{
    /// <summary>
    /// Creates an equality comparer for results that compares the values of two successes with the given comparer, for a <see cref="HashSet{T}"/>,
    /// a <see cref="Dictionary{TKey, TValue}"/> or <c>Distinct</c>. A failure equals another failure with the same error, and the comparer is not used for it.
    /// </summary>
    /// <typeparam name="T">The type of the values.</typeparam>
    /// <param name="valueComparer">Compares the values of two successes, and computes their hash codes.</param>
    /// <returns>An equality comparer for <see cref="Result{T}"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="valueComparer"/> is <see langword="null"/>.</exception>
    public static IEqualityComparer<Result<T>> CreateEqualityComparer<T>(IEqualityComparer<T> valueComparer)
    {
        Guard.NotNull(valueComparer, nameof(valueComparer));

        return new ResultEqualityComparer<T>(valueComparer);
    }
}
