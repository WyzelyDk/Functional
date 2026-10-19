namespace Wyzely.Functional;

/// <summary>
/// LINQ query syntax for <see cref="Result{T}"/> (<c>from a in ... from b in ... select ...</c>). <c>Zip</c>, which combines independent
/// results, is generated into <see cref="ResultExtensions"/>.
/// </summary>
/// <remarks>
/// Query syntax short-circuits at the first failure, like <see cref="ResultExtensions"/> <c>Bind</c> does. Use <c>Zip</c> or
/// <see cref="Result.Combine{T}(IEnumerable{Result{T}})"/> when every failure should be reported.
/// </remarks>
public static class ResultLinqExtensions
{
    /// <summary>Transforms the value of a successful result. This is <c>Map</c> under the name that query syntax requires.</summary>
    /// <typeparam name="T">The type of the value of the source.</typeparam>
    /// <typeparam name="TResult">The type of the transformed value.</typeparam>
    /// <param name="source">The result to transform.</param>
    /// <param name="selector">Computes the new value from the value of a successful result.</param>
    /// <returns>A successful result with the new value, or a failed result with the error of the source.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="selector"/> is <see langword="null"/>.</exception>
    public static Result<TResult> Select<T, TResult>(this Result<T> source, Func<T, TResult> selector) => source.Map(selector);

    /// <summary>Chains an operation that can fail. This is <c>Bind</c> under the name that query syntax requires.</summary>
    /// <typeparam name="T">The type of the value of the source.</typeparam>
    /// <typeparam name="TResult">The type of the value of the result that the selector returns.</typeparam>
    /// <param name="source">The result to continue from.</param>
    /// <param name="selector">The operation to run with the value of a successful result.</param>
    /// <returns>The result of the selector, or a failed result with the error of the source.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="selector"/> is <see langword="null"/>.</exception>
    public static Result<TResult> SelectMany<T, TResult>(this Result<T> source, Func<T, Result<TResult>> selector) => source.Bind(selector);

    /// <summary>Chains an operation that can fail and combines both values. The compiler uses this for a query with two <c>from</c> clauses.</summary>
    /// <typeparam name="T">The type of the value of the source.</typeparam>
    /// <typeparam name="TCollection">The type of the value of the intermediate result.</typeparam>
    /// <typeparam name="TResult">The type of the combined value.</typeparam>
    /// <param name="source">The result to continue from.</param>
    /// <param name="collectionSelector">The operation to run with the value of a successful source.</param>
    /// <param name="resultSelector">Combines the value of the source and the value of the intermediate result.</param>
    /// <returns>A successful result with the combined value, or a failed result with the first error.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="collectionSelector"/> or <paramref name="resultSelector"/> is <see langword="null"/>.
    /// </exception>
    public static Result<TResult> SelectMany<T, TCollection, TResult>(
        this Result<T> source,
        Func<T, Result<TCollection>> collectionSelector,
        Func<T, TCollection, TResult> resultSelector)
    {
        Guard.NotNull(collectionSelector, nameof(collectionSelector));
        Guard.NotNull(resultSelector, nameof(resultSelector));

        var sourceError = source.Unpack(out var sourceValue);

        if (sourceError is not null)
        {
            return Result.Failure<TResult>(sourceError);
        }

        var inner = collectionSelector(sourceValue);
        var innerError = inner.Unpack(out var innerValue);

        if (innerError is not null)
        {
            return Result.Failure<TResult>(innerError);
        }

        return Result.Success(resultSelector(sourceValue, innerValue));
    }
}
