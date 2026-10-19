using System.Diagnostics.CodeAnalysis;

namespace Wyzely.Functional;

/// <content>
/// <c>ToInvertedResult</c>: from a maybe to a result with the meaning turned around, for the question "this must not exist yet". A value is
/// the failure and the absence of a value is the success, so a chain continues only when nothing was found.
/// </content>
public static partial class MaybeResultExtensions
{
    /// <summary>
    /// Turns a maybe into a result with the meaning inverted: a value becomes a failure with the given error, and no value becomes a success. It is
    /// for checks that must find nothing, such as "no user has this email yet".
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="maybe">The maybe to convert.</param>
    /// <param name="error">The error of the failed result that is returned when the maybe has a value.</param>
    /// <returns>A failed result with <paramref name="error"/> when the maybe has a value, otherwise a successful result without a value.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="error"/> is <see langword="null"/>.</exception>
    public static Result ToInvertedResult<T>(this Maybe<T> maybe, Error error)
        where T : notnull
    {
        Guard.NotNull(error, nameof(error));

        return maybe.IsSome ? Result.Failure(error) : Result.Success();
    }

    /// <summary>
    /// Turns a maybe into a result with the meaning inverted, building the error from the value that was found, for example to say which user
    /// already has the email. The factory is not called when the maybe has no value.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="maybe">The maybe to convert.</param>
    /// <param name="error">Creates the error of the failed result from the value of the maybe.</param>
    /// <returns>
    /// A failed result with the error that <paramref name="error"/> created when the maybe has a value, otherwise a successful result
    /// without a value.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="error"/> is <see langword="null"/>, or it returned <see langword="null"/>.</exception>
    public static Result ToInvertedResult<T>(this Maybe<T> maybe, Func<T, Error> error)
        where T : notnull
    {
        Guard.NotNull(error, nameof(error));

        return maybe.TryGetValue(out var value) ? Result.Failure(error(value)) : Result.Success();
    }

    /// <summary>
    /// Turns the maybe that a task produces into a result with the meaning inverted: a value becomes a failure with the given error, and no
    /// value becomes a success.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="maybe">The task that produces the maybe. It must not be <see langword="null"/>.</param>
    /// <param name="error">The error of the failed result that is returned when the maybe has a value.</param>
    /// <returns>
    /// A task that produces a failed result with <paramref name="error"/> when the maybe has a value, otherwise a successful result without
    /// a value.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="maybe"/> or <paramref name="error"/> is <see langword="null"/>.</exception>
    public static Task<Result> ToInvertedResult<T>(this Task<Maybe<T>> maybe, Error error)
        where T : notnull
    {
        Guard.NotNull(maybe, nameof(maybe));
        Guard.NotNull(error, nameof(error));

        return maybe.Status == TaskStatus.RanToCompletion
            ? Task.FromResult(maybe.Result.ToInvertedResult(error))
            : maybe.Match(_ => Result.Failure(error), static () => Result.Success());
    }

    /// <summary>Turns the maybe that a task produces into a result with the meaning inverted, building the error from the value that was found.</summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="maybe">The task that produces the maybe. It must not be <see langword="null"/>.</param>
    /// <param name="error">Creates the error of the failed result from the value of the maybe. It is not called when the maybe has no value.</param>
    /// <returns>
    /// A task that produces a failed result with the error that <paramref name="error"/> created when the maybe has a value, otherwise a
    /// successful result without a value.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="maybe"/> or <paramref name="error"/> is <see langword="null"/>.</exception>
    public static Task<Result> ToInvertedResult<T>(this Task<Maybe<T>> maybe, Func<T, Error> error)
        where T : notnull
    {
        Guard.NotNull(maybe, nameof(maybe));
        Guard.NotNull(error, nameof(error));

        return maybe.Status == TaskStatus.RanToCompletion
            ? Task.FromResult(maybe.Result.ToInvertedResult(error))
            : maybe.Match(value => Result.Failure(error(value)), static () => Result.Success());
    }

    /// <summary>
    /// Turns the maybe that a value task produces into a result with the meaning inverted: a value becomes a failure with the given error,
    /// and no value becomes a success.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="maybe">The value task that produces the maybe.</param>
    /// <param name="error">The error of the failed result that is returned when the maybe has a value.</param>
    /// <returns>
    /// A value task that produces a failed result with <paramref name="error"/> when the maybe has a value, otherwise a successful result
    /// without a value.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="error"/> is <see langword="null"/>.</exception>
    [SuppressMessage("Sonar", "S5034", Justification = "IsCompletedSuccessfully is checked in the same expression.")]
    public static ValueTask<Result> ToInvertedResult<T>(this ValueTask<Maybe<T>> maybe, Error error)
        where T : notnull
    {
        Guard.NotNull(error, nameof(error));

        return maybe.IsCompletedSuccessfully
            ? new ValueTask<Result>(maybe.Result.ToInvertedResult(error))
            : maybe.Match(_ => Result.Failure(error), static () => Result.Success());
    }

    /// <summary>Turns the maybe that a value task produces into a result with the meaning inverted, building the error from the value that was found.</summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="maybe">The value task that produces the maybe.</param>
    /// <param name="error">Creates the error of the failed result from the value of the maybe. It is not called when the maybe has no value.</param>
    /// <returns>
    /// A value task that produces a failed result with the error that <paramref name="error"/> created when the maybe has a value,
    /// otherwise a successful result without a value.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="error"/> is <see langword="null"/>.</exception>
    [SuppressMessage("Sonar", "S5034", Justification = "IsCompletedSuccessfully is checked in the same expression.")]
    public static ValueTask<Result> ToInvertedResult<T>(this ValueTask<Maybe<T>> maybe, Func<T, Error> error)
        where T : notnull
    {
        Guard.NotNull(error, nameof(error));

        return maybe.IsCompletedSuccessfully
            ? new ValueTask<Result>(maybe.Result.ToInvertedResult(error))
            : maybe.Match(value => Result.Failure(error(value)), static () => Result.Success());
    }
}
