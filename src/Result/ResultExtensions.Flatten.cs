namespace Wyzely.Functional;

/// <content>
/// <c>Flatten</c>: removes one level of nesting from a result of a result.
/// </content>
public static partial class ResultExtensions
{
    /// <summary>Removes one level of nesting from a result of a result. This is <c>Bind</c> with the identity function.</summary>
    /// <typeparam name="T">The type of the value of the inner result.</typeparam>
    /// <param name="result">The nested result.</param>
    /// <returns>The inner result when the outer one succeeded, otherwise a failed result with the error of the outer one.</returns>
    public static Result<T> Flatten<T>(this Result<Result<T>> result) => result.Bind(static inner => inner);

    /// <summary>Removes one level of nesting from the result of a result that a task produces.</summary>
    /// <typeparam name="T">The type of the value of the inner result.</typeparam>
    /// <param name="result">The task that produces the nested result. It must not be <see langword="null"/>.</param>
    /// <returns>A task that produces the inner result when the outer one succeeded, otherwise a failed result with the error of the outer one.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="result"/> is <see langword="null"/>.</exception>
    public static Task<Result<T>> Flatten<T>(this Task<Result<Result<T>>> result) => result.Bind(static inner => inner);

    /// <summary>Removes one level of nesting from the result of a result that a value task produces.</summary>
    /// <typeparam name="T">The type of the value of the inner result.</typeparam>
    /// <param name="result">The value task that produces the nested result.</param>
    /// <returns>A value task that produces the inner result when the outer one succeeded, otherwise a failed result with the error of the outer one.</returns>
    public static ValueTask<Result<T>> Flatten<T>(this ValueTask<Result<Result<T>>> result) => result.Bind(static inner => inner);
}
