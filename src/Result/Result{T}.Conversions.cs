namespace Wyzely.Functional;

/// <content>
/// The implicit conversions: a value converts to a success that holds it, an <see cref="Error"/> to a failure that carries it,
/// and a typed result to an untyped one by dropping the value.
/// </content>
public readonly partial struct Result<T>
{
    /// <summary>
    /// Converts a value to a successful result that holds it.
    /// A <see langword="null"/> reference becomes a success that holds <see langword="null"/>.
    /// </summary>
    /// <param name="value">The value of the success.</param>
    /// <returns>A successful result that holds <paramref name="value"/>.</returns>
    public static implicit operator Result<T>(T value) => Result.Success(value);

    /// <summary>Converts an error to a failed result that carries it.</summary>
    /// <param name="error">The error of the failure.</param>
    /// <returns>A failed result with <paramref name="error"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="error"/> is <see langword="null"/>.</exception>
    public static implicit operator Result<T>(Error error) => Result.Failure<T>(error);

    /// <summary>Converts a typed result to an untyped one by dropping the value.</summary>
    /// <param name="result">The result to convert.</param>
    /// <returns>An untyped result in the same state.</returns>
    public static implicit operator Result(Result<T> result) => result.ToResult();

    /// <summary>Drops the value and keeps the state: a success stays a success and a failure keeps its error.</summary>
    /// <returns>An untyped result in the same state.</returns>
    public Result ToResult() => _error is null ? default : new Result(_error);
}
