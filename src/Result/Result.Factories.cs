using System.Diagnostics.CodeAnalysis;

namespace Wyzely.Functional;

/// <content>
/// The factory methods that create results. Results are always created here (or by the railway methods), never with
/// <see langword="default"/> or <c>new Result()</c>.
/// </content>
public readonly partial struct Result
{
    /// <summary>Creates a successful result without a value.</summary>
    /// <returns>A successful result.</returns>
    public static Result Success() => default;

    /// <summary>Creates a successful result with a value.</summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="value">The value of the result, which may be <see langword="null"/> for a nullable type.</param>
    /// <returns>A successful result that holds <paramref name="value"/>.</returns>
    public static Result<T> Success<T>(T value) => Result<T>.Succeeded(value);

    /// <summary>Creates a failed result.</summary>
    /// <param name="error">The error that describes the failure.</param>
    /// <returns>A failed result that holds <paramref name="error"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="error"/> is <see langword="null"/>.</exception>
    public static Result Failure(Error error) => new(Guard.NotNull(error, nameof(error)));

    /// <summary>Creates a failed result from a message.</summary>
    /// <param name="message">The message, or message template, of the error. It must not be empty.</param>
    /// <returns>A failed result with a new <see cref="Error"/> of kind <see cref="ErrorKind.Failure"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="message"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="message"/> is empty or consists only of white space.</exception>
    [SuppressMessage("Design", "WYZ0116", Justification = "This is the implementation of the overload that the rule suggests; calling it would recurse.")]
    public static Result Failure(string message) => Failure(Error.Create(message));

    /// <summary>Creates a failed result that would have held a value of type <typeparamref name="T"/>.</summary>
    /// <typeparam name="T">The type of the value the result would have held.</typeparam>
    /// <param name="error">The error that describes the failure.</param>
    /// <returns>A failed result that holds <paramref name="error"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="error"/> is <see langword="null"/>.</exception>
    public static Result<T> Failure<T>(Error error) => Result<T>.Failed(Guard.NotNull(error, nameof(error)));

    /// <summary>Creates a failed result from a message that would have held a value of type <typeparamref name="T"/>.</summary>
    /// <typeparam name="T">The type of the value the result would have held.</typeparam>
    /// <param name="message">The message, or message template, of the error. It must not be empty.</param>
    /// <returns>A failed result with a new <see cref="Error"/> of kind <see cref="ErrorKind.Failure"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="message"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="message"/> is empty or consists only of white space.</exception>
    [SuppressMessage("Design", "WYZ0116", Justification = "This is the implementation of the overload that the rule suggests; calling it would recurse.")]
    public static Result<T> Failure<T>(string message) => Failure<T>(Error.Create(message));

    /// <summary>Creates a failed result from an exception.</summary>
    /// <param name="exception">The exception to describe. It is kept as the cause of the error.</param>
    /// <returns>A failed result whose error was created by <see cref="Error.FromException(Exception, string?)"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="exception"/> is <see langword="null"/>.</exception>
    public static Result FromException(Exception exception) => Failure(ErrorOf(Guard.NotNull(exception, nameof(exception)), null));

    /// <summary>Creates a failed result from an exception that would have held a value of type <typeparamref name="T"/>.</summary>
    /// <typeparam name="T">The type of the value the result would have held.</typeparam>
    /// <param name="exception">The exception to describe. It is kept as the cause of the error.</param>
    /// <returns>A failed result whose error was created by <see cref="Error.FromException(Exception, string?)"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="exception"/> is <see langword="null"/>.</exception>
    public static Result<T> FromException<T>(Exception exception) => Failure<T>(ErrorOf(Guard.NotNull(exception, nameof(exception)), null));
}
