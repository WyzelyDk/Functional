namespace Wyzely.Functional;

/// <content>
/// The comparers for collections of maybes, which take the comparer of the values.
/// </content>
public static partial class Maybe
{
    /// <summary>
    /// Creates an equality comparer for maybes that compares the values with the given comparer, for a <see cref="HashSet{T}"/>,
    /// a <see cref="Dictionary{TKey, TValue}"/> or <c>Distinct</c>. Two maybes without a value are equal, and a maybe with a value never equals one without.
    /// </summary>
    /// <typeparam name="T">The type of the values.</typeparam>
    /// <param name="valueComparer">Compares the values of two maybes that both have one, and computes their hash codes.</param>
    /// <returns>An equality comparer for <see cref="Maybe{T}"/>.</returns>
    /// <remarks>
    /// A comparer of nullable values, such as a <see cref="StringComparer"/>, makes the compiler infer a nullable <typeparamref name="T"/>,
    /// which violates the constraint. Name the type argument: <c>CreateEqualityComparer&lt;string&gt;(StringComparer.Ordinal)</c>.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="valueComparer"/> is <see langword="null"/>.</exception>
    public static IEqualityComparer<Maybe<T>> CreateEqualityComparer<T>(IEqualityComparer<T> valueComparer)
        where T : notnull
    {
        Guard.NotNull(valueComparer, nameof(valueComparer));

        return new MaybeEqualityComparer<T>(valueComparer);
    }

    /// <summary>
    /// Creates a comparer for maybes that compares the values with the given comparer, for sorting. A maybe without a value sorts before one with a value,
    /// and two maybes without a value are equal.
    /// </summary>
    /// <typeparam name="T">The type of the values.</typeparam>
    /// <param name="valueComparer">Compares the values of two maybes that both have one.</param>
    /// <returns>A comparer for <see cref="Maybe{T}"/>.</returns>
    /// <remarks>
    /// A comparer of nullable values, such as a <see cref="StringComparer"/>, makes the compiler infer a nullable <typeparamref name="T"/>,
    /// which violates the constraint. Name the type argument: <c>CreateComparer&lt;string&gt;(StringComparer.Ordinal)</c>.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="valueComparer"/> is <see langword="null"/>.</exception>
    public static IComparer<Maybe<T>> CreateComparer<T>(IComparer<T> valueComparer)
        where T : notnull
    {
        Guard.NotNull(valueComparer, nameof(valueComparer));

        return new MaybeComparer<T>(valueComparer);
    }
}
