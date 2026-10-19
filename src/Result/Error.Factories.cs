namespace Wyzely.Functional;

/// <content>
/// The factory methods that create errors, one per kind, and the conversion of an exception into an error.
/// </content>
public sealed partial class Error
{
    /// <summary>Creates an error of kind <see cref="ErrorKind.Failure"/>.</summary>
    /// <param name="message">The message, or message template with <c>{Name}</c> placeholders. It must not be empty.</param>
    /// <returns>A new error.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="message"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="message"/> is empty or consists only of white space.</exception>
    public static Error Create(string message) => Create(message, default);

    /// <summary>Creates an error of the given kind.</summary>
    /// <param name="message">The message, or message template with <c>{Name}</c> placeholders. It must not be empty.</param>
    /// <param name="kind">The kind of the error.</param>
    /// <returns>A new error.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="message"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="message"/> is empty or consists only of white space.</exception>
    public static Error Create(string message, ErrorKind kind) =>
        new(Guard.NotNullOrWhiteSpace(message, nameof(message)), kind, null, false, null, NoDetails, NoContext, NoChildren);

    /// <summary>Creates an error of kind <see cref="ErrorKind.Unexpected"/>.</summary>
    /// <param name="message">The message, or message template. It must not be empty.</param>
    /// <returns>A new error.</returns>
    public static Error Unexpected(string message) => Create(message, ErrorKind.Unexpected);

    /// <summary>Creates an error of kind <see cref="ErrorKind.Validation"/>.</summary>
    /// <param name="message">The message, or message template. It must not be empty.</param>
    /// <returns>A new error.</returns>
    public static Error Validation(string message) => Create(message, ErrorKind.Validation);

    /// <summary>Creates an error of kind <see cref="ErrorKind.NotFound"/>.</summary>
    /// <param name="message">The message, or message template. It must not be empty.</param>
    /// <returns>A new error.</returns>
    public static Error NotFound(string message) => Create(message, ErrorKind.NotFound);

    /// <summary>Creates an error of kind <see cref="ErrorKind.Conflict"/>.</summary>
    /// <param name="message">The message, or message template. It must not be empty.</param>
    /// <returns>A new error.</returns>
    public static Error Conflict(string message) => Create(message, ErrorKind.Conflict);

    /// <summary>Creates an error of kind <see cref="ErrorKind.Unauthorized"/>.</summary>
    /// <param name="message">The message, or message template. It must not be empty.</param>
    /// <returns>A new error.</returns>
    public static Error Unauthorized(string message) => Create(message, ErrorKind.Unauthorized);

    /// <summary>Creates an error of kind <see cref="ErrorKind.Forbidden"/>.</summary>
    /// <param name="message">The message, or message template. It must not be empty.</param>
    /// <returns>A new error.</returns>
    public static Error Forbidden(string message) => Create(message, ErrorKind.Forbidden);

    /// <summary>Creates a transient error of kind <see cref="ErrorKind.Unavailable"/>.</summary>
    /// <param name="message">The message, or message template. It must not be empty.</param>
    /// <returns>A new error that is marked as transient.</returns>
    public static Error Unavailable(string message) => CreateTransient(message, ErrorKind.Unavailable);

    /// <summary>Creates a transient error of kind <see cref="ErrorKind.Timeout"/>.</summary>
    /// <param name="message">The message, or message template. It must not be empty.</param>
    /// <returns>A new error that is marked as transient.</returns>
    public static Error Timeout(string message) => CreateTransient(message, ErrorKind.Timeout);

    /// <summary>Creates an error of kind <see cref="ErrorKind.Cancelled"/>.</summary>
    /// <param name="message">
    /// The message, or message template. When omitted, <see cref="ResultOptions.CancelledMessage"/> is used (<c>The operation was
    /// cancelled.</c> by default).
    /// </param>
    /// <returns>A new error.</returns>
    public static Error Cancelled(string? message = null) => Create(message ?? ResultConfiguration.Current.CancelledMessage, ErrorKind.Cancelled);

    /// <summary>Creates an error from an exception and keeps the exception as the <see cref="Exception"/> of the error.</summary>
    /// <param name="exception">The exception to describe.</param>
    /// <param name="message">
    /// The message, or message template, of the error. When omitted, the message of the exception is used as plain text
    /// (braces in it are not treated as placeholders), or the type name of the exception when its message is empty.
    /// </param>
    /// <returns>
    /// A new error. A <see cref="OperationCanceledException"/> yields <see cref="ErrorKind.Cancelled"/>, a
    /// <see cref="TimeoutException"/> a transient <see cref="ErrorKind.Timeout"/>, and any other exception <see cref="ErrorKind.Unexpected"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="exception"/> is <see langword="null"/>.</exception>
    public static Error FromException(Exception exception, string? message = null)
    {
        Guard.NotNull(exception, nameof(exception));
        var template = message ?? EscapeBraces(MessageOf(exception));

        return new(
            Guard.NotNullOrWhiteSpace(template, nameof(message)),
            KindOf(exception),
            null,
            exception is TimeoutException,
            exception,
            NoDetails,
            NoContext,
            NoChildren);
    }

    private static Error CreateTransient(string message, ErrorKind kind) =>
        new(Guard.NotNullOrWhiteSpace(message, nameof(message)), kind, null, true, null, NoDetails, NoContext, NoChildren);

    // The message is read once: some exceptions, such as ArgumentException, build it again on every read.
    private static string MessageOf(Exception exception)
    {
        var message = exception.Message;

        return string.IsNullOrWhiteSpace(message) ? exception.GetType().Name : message;
    }

    private static ErrorKind KindOf(Exception exception) => exception switch
    {
        OperationCanceledException => ErrorKind.Cancelled,
        TimeoutException => ErrorKind.Timeout,
        _ => ErrorKind.Unexpected,
    };

    private static string EscapeBraces(string text) => text.Replace("{", "{{").Replace("}", "}}");
}
