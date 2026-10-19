namespace Wyzely.Functional;

/// <summary>
/// The exception thrown when the value of a failed result is read, or when <c>ThrowIfFailure</c> is called on a failed result.
/// </summary>
/// <remarks>
/// The <see cref="Error"/> that made the result fail is available as a property, and the exception of that error, when it
/// has one, becomes the <see cref="Exception.InnerException"/>. The message contains the redacted description of the error, so
/// sensitive details never end up in logs by accident.
/// </remarks>
/// <param name="error">The error of the failed result. <see langword="null"/> throws <see cref="ArgumentNullException"/>.</param>
public sealed class ResultFailedException(Error error) : InvalidOperationException(Describe(error), error.Exception)
{
    /// <summary>Gets the error of the failed result.</summary>
    public Error Error { get; } = error;

    private static string Describe(Error error) =>
        "The result failed: " + Guard.NotNull(error, nameof(error)).ToRedactedString();
}
