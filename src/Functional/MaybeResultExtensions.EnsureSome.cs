using System.Diagnostics.CodeAnalysis;

namespace Wyzely.Functional;

/// <content>
/// <c>EnsureSome</c>: from a result that holds a maybe to a result that holds the value, where an absent value becomes the error you give. It
/// does for <c>Result&lt;Maybe&lt;T&gt;&gt;</c> what <c>EnsureNotNull</c> does for <c>Result&lt;T?&gt;</c>.
/// </content>
public static partial class MaybeResultExtensions
{
    /// <summary>Turns a result that holds a maybe into a result that holds the value: a maybe with no value becomes a failure with the given error.</summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="result">The result to convert. A failed result passes through with its own error.</param>
    /// <param name="error">The error of the failed result that is returned when the maybe has no value.</param>
    /// <returns>A successful result with the value, or a failed result with the error of the receiver or with <paramref name="error"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="error"/> is <see langword="null"/>.</exception>
    public static Result<T> EnsureSome<T>(this Result<Maybe<T>> result, Error error)
        where T : notnull
    {
        Guard.NotNull(error, nameof(error));

        return result.TryGetValue(out var maybe) ? maybe.ToResult(error) : result.ConvertFailure<T>();
    }

    /// <summary>Turns a result that holds a maybe into a result that holds the value, creating the error only when there is no value.</summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="result">The result to convert. A failed result passes through with its own error.</param>
    /// <param name="error">Creates the error of the failed result. It is not called when the maybe has a value or the receiver failed.</param>
    /// <returns>
    /// A successful result with the value, or a failed result with the error of the receiver or the one that <paramref name="error"/> created.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="error"/> is <see langword="null"/>, or it returned <see langword="null"/>.</exception>
    public static Result<T> EnsureSome<T>(this Result<Maybe<T>> result, Func<Error> error)
        where T : notnull
    {
        Guard.NotNull(error, nameof(error));

        return result.TryGetValue(out var maybe) ? maybe.ToResult(error) : result.ConvertFailure<T>();
    }

    /// <summary>Turns the result of a task that holds a maybe into a result that holds the value, with the given error when the maybe has no value.</summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="result">The task that produces the result. It must not be <see langword="null"/>.</param>
    /// <param name="error">The error of the failed result that is returned when the maybe has no value.</param>
    /// <returns>A task that produces a successful result with the value, or a failed result.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="result"/> or <paramref name="error"/> is <see langword="null"/>.</exception>
    public static Task<Result<T>> EnsureSome<T>(this Task<Result<Maybe<T>>> result, Error error)
        where T : notnull
    {
        Guard.NotNull(result, nameof(result));
        Guard.NotNull(error, nameof(error));

        return result.Status == TaskStatus.RanToCompletion
            ? Task.FromResult(result.Result.EnsureSome(error))
            : result.Bind(maybe => maybe.ToResult(error));
    }

    /// <summary>Turns the result of a task that holds a maybe into a result that holds the value, creating the error only when there is no value.</summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="result">The task that produces the result. It must not be <see langword="null"/>.</param>
    /// <param name="error">Creates the error of the failed result. It is not called when the maybe has a value or the receiver failed.</param>
    /// <returns>A task that produces a successful result with the value, or a failed result.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="result"/> or <paramref name="error"/> is <see langword="null"/>.</exception>
    public static Task<Result<T>> EnsureSome<T>(this Task<Result<Maybe<T>>> result, Func<Error> error)
        where T : notnull
    {
        Guard.NotNull(result, nameof(result));
        Guard.NotNull(error, nameof(error));

        return result.Status == TaskStatus.RanToCompletion
            ? Task.FromResult(result.Result.EnsureSome(error))
            : result.Bind(maybe => maybe.ToResult(error));
    }

    /// <summary>
    /// Turns the result of a value task that holds a maybe into a result that holds the value, with the given error when the maybe has no value.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="result">The value task that produces the result.</param>
    /// <param name="error">The error of the failed result that is returned when the maybe has no value.</param>
    /// <returns>A value task that produces a successful result with the value, or a failed result.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="error"/> is <see langword="null"/>.</exception>
    [SuppressMessage("Sonar", "S5034", Justification = "IsCompletedSuccessfully is checked in the same expression.")]
    public static ValueTask<Result<T>> EnsureSome<T>(this ValueTask<Result<Maybe<T>>> result, Error error)
        where T : notnull
    {
        Guard.NotNull(error, nameof(error));

        return result.IsCompletedSuccessfully
            ? new ValueTask<Result<T>>(result.Result.EnsureSome(error))
            : result.Bind(maybe => maybe.ToResult(error));
    }

    /// <summary>
    /// Turns the result of a value task that holds a maybe into a result that holds the value, creating the error only when there is no value.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="result">The value task that produces the result.</param>
    /// <param name="error">Creates the error of the failed result. It is not called when the maybe has a value or the receiver failed.</param>
    /// <returns>A value task that produces a successful result with the value, or a failed result.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="error"/> is <see langword="null"/>.</exception>
    [SuppressMessage("Sonar", "S5034", Justification = "IsCompletedSuccessfully is checked in the same expression.")]
    public static ValueTask<Result<T>> EnsureSome<T>(this ValueTask<Result<Maybe<T>>> result, Func<Error> error)
        where T : notnull
    {
        Guard.NotNull(error, nameof(error));

        return result.IsCompletedSuccessfully
            ? new ValueTask<Result<T>>(result.Result.EnsureSome(error))
            : result.Bind(maybe => maybe.ToResult(error));
    }
}
