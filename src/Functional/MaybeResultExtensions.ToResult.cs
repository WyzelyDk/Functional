using System.Diagnostics.CodeAnalysis;

namespace Wyzely.Functional;

/// <content>
/// <c>ToResult</c>: from a maybe to a result, where an absent value becomes the error you give.
/// </content>
public static partial class MaybeResultExtensions
{
    /// <summary>Turns a maybe into a result: a value becomes a success, and no value becomes a failure with the given error.</summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="maybe">The maybe to convert.</param>
    /// <param name="error">The error of the failed result that is returned when the maybe has no value.</param>
    /// <returns>A successful result with the value, or a failed result with <paramref name="error"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="error"/> is <see langword="null"/>.</exception>
    public static Result<T> ToResult<T>(this Maybe<T> maybe, Error error)
        where T : notnull
    {
        Guard.NotNull(error, nameof(error));

        return maybe.TryGetValue(out var value) ? Result.Success(value) : Result.Failure<T>(error);
    }

    /// <summary>Turns a maybe into a result, creating the error only when there is no value.</summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="maybe">The maybe to convert.</param>
    /// <param name="error">Creates the error of the failed result. It is not called when the maybe has a value.</param>
    /// <returns>A successful result with the value, or a failed result with the error that <paramref name="error"/> created.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="error"/> is <see langword="null"/>, or it returned <see langword="null"/>.</exception>
    public static Result<T> ToResult<T>(this Maybe<T> maybe, Func<Error> error)
        where T : notnull
    {
        Guard.NotNull(error, nameof(error));

        return maybe.TryGetValue(out var value) ? Result.Success(value) : Result.Failure<T>(error());
    }

    /// <summary>Turns the maybe that a task produces into a result: a value becomes a success, and no value becomes a failure with the given error.</summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="maybe">The task that produces the maybe. It must not be <see langword="null"/>.</param>
    /// <param name="error">The error of the failed result that is returned when the maybe has no value.</param>
    /// <returns>A task that produces a successful result with the value, or a failed result with <paramref name="error"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="maybe"/> or <paramref name="error"/> is <see langword="null"/>.</exception>
    public static Task<Result<T>> ToResult<T>(this Task<Maybe<T>> maybe, Error error)
        where T : notnull
    {
        Guard.NotNull(maybe, nameof(maybe));
        Guard.NotNull(error, nameof(error));

        return maybe.Status == TaskStatus.RanToCompletion
            ? Task.FromResult(maybe.Result.ToResult(error))
            : maybe.Match(static value => Result.Success(value), () => Result.Failure<T>(error));
    }

    /// <summary>Turns the maybe that a task produces into a result, creating the error only when there is no value.</summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="maybe">The task that produces the maybe. It must not be <see langword="null"/>.</param>
    /// <param name="error">Creates the error of the failed result. It is not called when the maybe has a value.</param>
    /// <returns>A task that produces a successful result with the value, or a failed result with the error that <paramref name="error"/> created.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="maybe"/> or <paramref name="error"/> is <see langword="null"/>.</exception>
    public static Task<Result<T>> ToResult<T>(this Task<Maybe<T>> maybe, Func<Error> error)
        where T : notnull
    {
        Guard.NotNull(maybe, nameof(maybe));
        Guard.NotNull(error, nameof(error));

        return maybe.Status == TaskStatus.RanToCompletion
            ? Task.FromResult(maybe.Result.ToResult(error))
            : maybe.Match(static value => Result.Success(value), () => Result.Failure<T>(error()));
    }

    /// <summary>
    /// Turns the maybe that a value task produces into a result: a value becomes a success, and no value becomes a failure with the given
    /// error.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="maybe">The value task that produces the maybe.</param>
    /// <param name="error">The error of the failed result that is returned when the maybe has no value.</param>
    /// <returns>A value task that produces a successful result with the value, or a failed result with <paramref name="error"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="error"/> is <see langword="null"/>.</exception>
    [SuppressMessage("Sonar", "S5034", Justification = "IsCompletedSuccessfully is checked in the same expression.")]
    public static ValueTask<Result<T>> ToResult<T>(this ValueTask<Maybe<T>> maybe, Error error)
        where T : notnull
    {
        Guard.NotNull(error, nameof(error));

        return maybe.IsCompletedSuccessfully
            ? new ValueTask<Result<T>>(maybe.Result.ToResult(error))
            : maybe.Match(static value => Result.Success(value), () => Result.Failure<T>(error));
    }

    /// <summary>Turns the maybe that a value task produces into a result, creating the error only when there is no value.</summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="maybe">The value task that produces the maybe.</param>
    /// <param name="error">Creates the error of the failed result. It is not called when the maybe has a value.</param>
    /// <returns>
    /// A value task that produces a successful result with the value, or a failed result with the error that <paramref name="error"/>
    /// created.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="error"/> is <see langword="null"/>.</exception>
    [SuppressMessage("Sonar", "S5034", Justification = "IsCompletedSuccessfully is checked in the same expression.")]
    public static ValueTask<Result<T>> ToResult<T>(this ValueTask<Maybe<T>> maybe, Func<Error> error)
        where T : notnull
    {
        Guard.NotNull(error, nameof(error));

        return maybe.IsCompletedSuccessfully
            ? new ValueTask<Result<T>>(maybe.Result.ToResult(error))
            : maybe.Match(static value => Result.Success(value), () => Result.Failure<T>(error()));
    }
}
