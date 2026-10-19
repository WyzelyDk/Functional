using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Wyzely.Functional;

/// <summary>
/// Describes why an operation failed. An error is immutable: every <c>With...</c> method returns a modified copy.
/// </summary>
/// <remarks>
/// <para>
/// An error carries a human-readable message, an optional stable <see cref="Code"/>, a <see cref="Kind"/>, a
/// <see cref="IsTransient"/> flag for retry decisions, an optional cause (<see cref="Exception"/>), structured
/// <see cref="Details"/> and <see cref="Context"/> frames that tell where the failure travelled through.
/// </para>
/// <para>
/// The message of an error is a template: <c>"Upload to {Path} failed"</c> combined with
/// <c>.WithDetail("Path", path)</c> renders as <c>Upload to a/b.json failed</c>. The template stays available as a stable
/// identity for grouping and logging, and the values stay available as structured data.
/// </para>
/// <para>
/// Several failures are represented by one error as well: <see cref="Aggregate(IEnumerable{Error})"/> returns an error whose
/// <see cref="Children"/> hold the individual errors, so a <see cref="Result"/> only ever has to carry a single error.
/// </para>
/// </remarks>
[DebuggerDisplay("{ToString(),nq}")]
public sealed partial class Error : IEquatable<Error>
{
    private static readonly IReadOnlyList<ErrorDetail> NoDetails = new ReadOnlyCollection<ErrorDetail>(Array.Empty<ErrorDetail>());
    private static readonly IReadOnlyList<string> NoContext = new ReadOnlyCollection<string>(Array.Empty<string>());
    private static readonly IReadOnlyList<Error> NoChildren = new ReadOnlyCollection<Error>(Array.Empty<Error>());

    [SuppressMessage("Style", "IDE0032", Justification = "The message is rendered lazily on first use, so it cannot be an auto-property.")]
    private string? _message;
    [SuppressMessage("Style", "IDE0032", Justification = "The message is rendered lazily on first use, so it cannot be an auto-property.")]
    private string? _redactedMessage;

    [SuppressMessage(
        "Sonar",
        "S107",
        Justification = "Every field of this immutable error is set here in one go (one allocation); grouping them would only add an indirection.")]
    private Error(
        string template,
        ErrorKind kind,
        string? code,
        bool isTransient,
        Exception? exception,
        IReadOnlyList<ErrorDetail> details,
        IReadOnlyList<string> context,
        IReadOnlyList<Error> children)
    {
        Template = template;
        Kind = kind;
        Code = code;
        IsTransient = isTransient;
        Exception = exception;
        Details = details;
        Context = context;
        Children = children;
    }

    /// <summary>Gets the message template of the error, for example <c>Upload to {Path} failed</c>. It is a stable identity for grouping.</summary>
    /// <remarks>For an aggregate error this is a fixed summary text; the individual messages are in <see cref="Children"/>.</remarks>
    public string Template { get; }

    /// <summary>Gets the message of the error with every placeholder replaced by the value of the detail with the same name.</summary>
    /// <remarks>
    /// A placeholder without a matching detail is kept as written. <c>{{</c> and <c>}}</c> produce a single brace. The context
    /// frames are not part of the message; <see cref="ToString"/> includes them. For an aggregate error the message joins the
    /// descriptions of the children with <see cref="ResultOptions.ChildSeparator"/> (<c>, </c> by default).
    /// </remarks>
    public string Message => _message ??= BuildMessage(redact: false);

    /// <summary>
    /// Gets the message like <see cref="Message"/>, but with the value of every sensitive detail replaced by
    /// <see cref="ResultOptions.RedactedValue"/> (<c>***</c> by default).
    /// </summary>
    public string RedactedMessage => _redactedMessage ??= BuildMessage(redact: true);

    /// <summary>Gets the optional, stable, machine-readable code of the error, or <see langword="null"/>.</summary>
    public string? Code { get; }

    /// <summary>Gets the class of the failure. It is <see cref="ErrorKind.Failure"/> unless a more specific kind was set.</summary>
    public ErrorKind Kind { get; }

    /// <summary>Gets a value indicating whether retrying the operation later might succeed.</summary>
    public bool IsTransient { get; }

    /// <summary>Gets the exception that caused the error, or <see langword="null"/> when there is none.</summary>
    public Exception? Exception { get; }

    /// <summary>Gets the structured details of the error, in the order they were added.</summary>
    public IReadOnlyList<ErrorDetail> Details { get; }

    /// <summary>Gets the context frames of the error, outermost first.</summary>
    /// <remarks>A frame is added by <see cref="WithContext(string)"/> each time the error passes a layer that knows more about the situation.</remarks>
    public IReadOnlyList<string> Context { get; }

    /// <summary>Gets the individual errors of an aggregate error. It is empty for an error that is not an aggregate.</summary>
    public IReadOnlyList<Error> Children { get; }

    /// <summary>Gets a value indicating whether this error combines several individual errors.</summary>
    public bool IsAggregate => Children.Count > 0;
}
