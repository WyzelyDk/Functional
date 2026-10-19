using System.Diagnostics.CodeAnalysis;

namespace Wyzely.Functional;

/// <content>
/// <c>ToMaybe</c>: from a result to a maybe, where a failure becomes an absent value and its error is dropped.
/// </content>
public static partial class MaybeResultExtensions
{
    /// <summary>Turns a result into a maybe: a success becomes a value and a failure becomes none. The error of a failure is dropped.</summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="result">The result to convert.</param>
    /// <returns>A maybe that holds the value of a success, or none for a failure or for a success whose value is <see langword="null"/>.</returns>
    public static Maybe<T> ToMaybe<T>(this Result<T> result)
        where T : notnull => result.TryGetValue(out var value) ? (Maybe<T>)value : default;

    /// <summary>
    /// Turns the result that a task produces into a maybe: a success becomes a value and a failure becomes none. The error of a failure is
    /// dropped.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="result">The task that produces the result. It must not be <see langword="null"/>.</param>
    /// <returns>A task that produces a maybe with the value of a success, or none for a failure.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="result"/> is <see langword="null"/>.</exception>
    public static Task<Maybe<T>> ToMaybe<T>(this Task<Result<T>> result)
        where T : notnull
    {
        Guard.NotNull(result, nameof(result));

        return result.Status == TaskStatus.RanToCompletion
            ? Task.FromResult(result.Result.ToMaybe())
            : result.Match(static value => (Maybe<T>)value, static _ => default);
    }

    /// <summary>
    /// Turns the result that a value task produces into a maybe: a success becomes a value and a failure becomes none. The error of a
    /// failure is dropped.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="result">The value task that produces the result.</param>
    /// <returns>A value task that produces a maybe with the value of a success, or none for a failure.</returns>
    [SuppressMessage("Sonar", "S5034", Justification = "IsCompletedSuccessfully is checked in the same expression.")]
    public static ValueTask<Maybe<T>> ToMaybe<T>(this ValueTask<Result<T>> result)
        where T : notnull =>
        result.IsCompletedSuccessfully
            ? new ValueTask<Maybe<T>>(result.Result.ToMaybe())
            : result.Match(static value => (Maybe<T>)value, static _ => default);
}
