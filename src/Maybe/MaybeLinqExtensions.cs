namespace Wyzely.Functional;

/// <summary>
/// LINQ query syntax for <see cref="Maybe{T}"/> (<c>from a in ... from b in ... where ... select ...</c>). <c>Zip</c>, which combines
/// independent maybes, is generated into <see cref="MaybeExtensions"/>, and <c>Flatten</c> lives there too.
/// </summary>
/// <remarks>
/// Query syntax short-circuits at the first maybe without a value, like <see cref="MaybeExtensions"/> <c>Bind</c> does.
/// </remarks>
public static class MaybeLinqExtensions
{
    /// <summary>Transforms the value of a maybe. This is <c>Map</c> under the name that query syntax requires.</summary>
    /// <typeparam name="T">The type of the value of the source.</typeparam>
    /// <typeparam name="TResult">The type of the transformed value.</typeparam>
    /// <param name="source">The maybe to transform.</param>
    /// <param name="selector">Computes the new value from the value of the maybe.</param>
    /// <returns>A maybe with the new value, or none when the source has no value.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="selector"/> is <see langword="null"/>.</exception>
    public static Maybe<TResult> Select<T, TResult>(this Maybe<T> source, Func<T, TResult?> selector)
        where T : notnull
        where TResult : notnull => source.Map(selector);

    /// <summary>
    /// Transforms the value of a maybe with a selector that returns a nullable value type. A <see langword="null"/> result becomes none, so
    /// the result is a <c>Maybe&lt;TResult&gt;</c>, never a <c>Maybe&lt;TResult?&gt;</c>.
    /// </summary>
    /// <typeparam name="T">The type of the value of the source.</typeparam>
    /// <typeparam name="TResult">The value type of the transformed value.</typeparam>
    /// <param name="source">The maybe to transform.</param>
    /// <param name="selector">Computes the new value from the value of the maybe. It may return <see langword="null"/>, which becomes none.</param>
    /// <returns>A maybe with the new value, or none when the source has no value or the selector returned <see langword="null"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="selector"/> is <see langword="null"/>.</exception>
    public static Maybe<TResult> Select<T, TResult>(this Maybe<T> source, Func<T, TResult?> selector)
        where T : notnull
        where TResult : struct => source.Map(selector);

    /// <summary>Chains an operation that may produce no value. This is <c>Bind</c> under the name that query syntax requires.</summary>
    /// <typeparam name="T">The type of the value of the source.</typeparam>
    /// <typeparam name="TResult">The type of the value of the maybe that the selector returns.</typeparam>
    /// <param name="source">The maybe to continue from.</param>
    /// <param name="selector">The operation to run with the value of the maybe.</param>
    /// <returns>The maybe of the selector, or none when the source has no value.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="selector"/> is <see langword="null"/>.</exception>
    public static Maybe<TResult> SelectMany<T, TResult>(this Maybe<T> source, Func<T, Maybe<TResult>> selector)
        where T : notnull
        where TResult : notnull => source.Bind(selector);

    /// <summary>
    /// Chains an operation that may produce no value and combines both values. The compiler uses this for a query with two <c>from</c>
    /// clauses.
    /// </summary>
    /// <typeparam name="T">The type of the value of the source.</typeparam>
    /// <typeparam name="TCollection">The type of the value of the intermediate maybe.</typeparam>
    /// <typeparam name="TResult">The type of the combined value.</typeparam>
    /// <param name="source">The maybe to continue from.</param>
    /// <param name="collectionSelector">The operation to run with the value of the source.</param>
    /// <param name="resultSelector">Combines the value of the source and the value of the intermediate maybe.</param>
    /// <returns>A maybe with the combined value, or none when either maybe has no value.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="collectionSelector"/> or <paramref name="resultSelector"/> is <see langword="null"/>.
    /// </exception>
    public static Maybe<TResult> SelectMany<T, TCollection, TResult>(
        this Maybe<T> source,
        Func<T, Maybe<TCollection>> collectionSelector,
        Func<T, TCollection, TResult?> resultSelector)
        where T : notnull
        where TCollection : notnull
        where TResult : notnull
    {
        Guard.NotNull(collectionSelector, nameof(collectionSelector));
        Guard.NotNull(resultSelector, nameof(resultSelector));

        if (!source.Unpack(out var sourceValue))
        {
            return default;
        }

        return collectionSelector(sourceValue).Unpack(out var innerValue)
            ? Maybe<TResult>.Of(resultSelector(sourceValue, innerValue))
            : default;
    }

    /// <summary>
    /// Chains an operation that may produce no value and combines both values with a selector that returns a nullable value type. A
    /// <see langword="null"/> result becomes none.
    /// </summary>
    /// <typeparam name="T">The type of the value of the source.</typeparam>
    /// <typeparam name="TCollection">The type of the value of the intermediate maybe.</typeparam>
    /// <typeparam name="TResult">The value type of the combined value.</typeparam>
    /// <param name="source">The maybe to continue from.</param>
    /// <param name="collectionSelector">The operation to run with the value of the source.</param>
    /// <param name="resultSelector">Combines the two values. It may return <see langword="null"/>, which becomes none.</param>
    /// <returns>A maybe with the combined value, or none when either maybe has no value or the selector returned <see langword="null"/>.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="collectionSelector"/> or <paramref name="resultSelector"/> is <see langword="null"/>.
    /// </exception>
    public static Maybe<TResult> SelectMany<T, TCollection, TResult>(
        this Maybe<T> source,
        Func<T, Maybe<TCollection>> collectionSelector,
        Func<T, TCollection, TResult?> resultSelector)
        where T : notnull
        where TCollection : notnull
        where TResult : struct
    {
        Guard.NotNull(collectionSelector, nameof(collectionSelector));
        Guard.NotNull(resultSelector, nameof(resultSelector));

        if (!source.Unpack(out var sourceValue) || !collectionSelector(sourceValue).Unpack(out var innerValue))
        {
            return default;
        }

        var value = resultSelector(sourceValue, innerValue);

        return value.HasValue ? new Maybe<TResult>(value.GetValueOrDefault()) : default;
    }

    /// <summary>Keeps the value of a maybe only when it satisfies a condition. This is <c>Filter</c> under the name that query syntax requires.</summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="source">The maybe to filter.</param>
    /// <param name="predicate">The condition the value must satisfy.</param>
    /// <returns>The source when it has a value that satisfies the condition, otherwise none.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="predicate"/> is <see langword="null"/>.</exception>
    public static Maybe<T> Where<T>(this Maybe<T> source, Func<T, bool> predicate)
        where T : notnull => source.Filter(predicate);
}
