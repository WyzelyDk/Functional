namespace Wyzely.Functional;

/// <summary>
/// The settings of the library that a consumer may want to change. Every setting has a default that is sensible for most
/// applications, so nothing has to be configured; change a setting once, at application start, with
/// <see cref="ResultConfiguration.Configure(Action{ResultOptions})"/>.
/// </summary>
/// <remarks>
/// The settings that shape text (messages, separators, the redaction text) are read when an error is created or rendered, so
/// configure them before the first error is created: an error keeps the text it has already rendered.
/// </remarks>
public sealed class ResultOptions
{
    /// <summary>
    /// Gets or sets the message template of an error that combines several errors (<see cref="Error.Aggregate(IEnumerable{Error})"/>). The
    /// default is <c>One or more errors occurred.</c>.
    /// </summary>
    /// <exception cref="ArgumentNullException">The value is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">The value is empty or white space.</exception>
    public string AggregateMessage
    {
        get;
        set => field = Guard.NotNullOrWhiteSpace(value, nameof(value));
    } = "One or more errors occurred.";

    /// <summary>
    /// Gets or sets the message of <see cref="Error.Cancelled(string?)"/> when no message is given. The default is <c>The operation was
    /// cancelled.</c>.
    /// </summary>
    /// <exception cref="ArgumentNullException">The value is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">The value is empty or white space.</exception>
    public string CancelledMessage
    {
        get;
        set => field = Guard.NotNullOrWhiteSpace(value, nameof(value));
    } = "The operation was cancelled.";

    /// <summary>
    /// Gets or sets the text that separates the messages of the children when an aggregate error is shown as one message. The default is
    /// <c>, </c>.
    /// </summary>
    /// <exception cref="ArgumentNullException">The value is <see langword="null"/>.</exception>
    public string ChildSeparator
    {
        get;
        set => field = Guard.NotNull(value, nameof(value));
    } = ", ";

    /// <summary>
    /// Gets or sets the text that separates the context frames (<see cref="Error.WithContext(string)"/>) from each other and from the
    /// message. The default is <c>: </c>.
    /// </summary>
    /// <exception cref="ArgumentNullException">The value is <see langword="null"/>.</exception>
    public string ContextSeparator
    {
        get;
        set => field = Guard.NotNull(value, nameof(value));
    } = ": ";

    /// <summary>Gets or sets the text that replaces the value of a sensitive error detail in redacted output. The default is <c>***</c>.</summary>
    /// <exception cref="ArgumentNullException">The value is <see langword="null"/>.</exception>
    public string RedactedValue
    {
        get;
        set => field = Guard.NotNull(value, nameof(value));
    } = "***";

    /// <summary>
    /// Gets or sets how the library turns an exception into an <see cref="Error"/> when it has to decide by itself: in
    /// <see cref="Result.Try(Action, Func{Exception, Error}?)"/> without an <c>onException</c> argument and in
    /// <see cref="Result.FromException(Exception)"/>. The default is <see cref="Error.FromException(Exception, string?)"/>.
    /// </summary>
    /// <exception cref="ArgumentNullException">The value is <see langword="null"/>.</exception>
    public Func<Exception, Error> ExceptionMapper
    {
        get;
        set => field = Guard.NotNull(value, nameof(value));
    } = static exception => Error.FromException(exception);

    /// <summary>
    /// Gets or sets which exceptions <see cref="Result.Try(Action, Func{Exception, Error}?)"/> turns into failed results; any other
    /// exception escapes. The default catches everything except <see cref="OperationCanceledException"/>, so that cancelling
    /// an operation is not mistaken for a failure of the operation.
    /// </summary>
    /// <exception cref="ArgumentNullException">The value is <see langword="null"/>.</exception>
    public Func<Exception, bool> ShouldCatch
    {
        get;
        set => field = Guard.NotNull(value, nameof(value));
    } = static exception => exception is not OperationCanceledException;

    /// <summary>
    /// Gets or sets the degree of parallelism that <see cref="ResultParallelExtensions"/> uses when the caller does not pass one:
    /// a positive number, or <c>-1</c> for no limit (the default).
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">The value is zero or less than <c>-1</c>.</exception>
    public int DefaultMaxDegreeOfParallelism
    {
        get;
        set
        {
            ResultParallelExtensions.ValidateDegree(value, nameof(value));
            field = value;
        }
    } = -1;

    /// <summary>
    /// Gets or sets a value indicating whether the asynchronous methods of the library continue on the captured synchronization context after an
    /// <see langword="await"/>. The default is <see langword="false"/>, which is right for a library and avoids deadlocks; set it to
    /// <see langword="true"/> only in an application that depends on the context (for example a UI thread) inside the
    /// delegates that it passes in.
    /// </summary>
    public bool ContinueOnCapturedContext { get; set; }

    /// <summary>Creates a copy, so that the active settings can never be changed through an instance that a caller still holds.</summary>
    /// <returns>A new instance with the same values.</returns>
    internal ResultOptions Clone() => (ResultOptions)MemberwiseClone();
}
