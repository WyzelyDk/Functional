using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Wyzely.Functional;

/// <summary>
/// One frame of the context of an <see cref="Error"/>: what a layer was doing when the error passed it, written as a message template with
/// details of its own, like the message of an error.
/// </summary>
/// <remarks>
/// <para>
/// <c>"Checkout of order {OrderId}"</c> combined with the detail <c>OrderId</c> renders as <c>Checkout of order 7</c>. The template stays
/// available as a stable text, and the values stay available as structured data. A detail marked as sensitive is replaced by
/// <see cref="ResultOptions.RedactedValue"/> in <see cref="RedactedMessage"/>, which is what <see cref="Error.ToRedactedString"/> prints.
/// </para>
/// <para>
/// The placeholders of a frame are filled from the details of the frame only, never from the details of the error, so two layers can use
/// the same name for different values.
/// </para>
/// </remarks>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class ErrorFrame : IEquatable<ErrorFrame>
{
    [SuppressMessage("Style", "IDE0032", Justification = "The message is rendered lazily on first use, so it cannot be an auto-property.")]
    private string? _message;
    [SuppressMessage("Style", "IDE0032", Justification = "The message is rendered lazily on first use, so it cannot be an auto-property.")]
    private string? _redactedMessage;

    /// <summary>Initializes a new instance of the <see cref="ErrorFrame"/> class.</summary>
    /// <param name="template">The message template of the frame.</param>
    /// <param name="details">The details that fill the placeholders of the template.</param>
    internal ErrorFrame(string template, IReadOnlyList<ErrorDetail> details)
    {
        Template = template;
        Details = details;
    }

    /// <summary>Gets the message template of the frame, for example <c>Checkout of order {OrderId}</c>.</summary>
    public string Template { get; }

    /// <summary>Gets the structured details of the frame, in the order they were given.</summary>
    public IReadOnlyList<ErrorDetail> Details { get; }

    /// <summary>Gets the text of the frame with every placeholder replaced by the value of the detail with the same name.</summary>
    /// <remarks>A placeholder without a matching detail is kept as written. <c>{{</c> and <c>}}</c> produce a single brace.</remarks>
    public string Message => _message ??= Error.Format(Template, Details, redact: false);

    /// <summary>
    /// Gets the text of the frame like <see cref="Message"/>, but with the value of every sensitive detail replaced by
    /// <see cref="ResultOptions.RedactedValue"/>.
    /// </summary>
    public string RedactedMessage => _redactedMessage ??= Error.Format(Template, Details, redact: true);

    /// <summary>Determines whether another frame has the same template and the same details.</summary>
    /// <param name="other">The frame to compare with.</param>
    /// <returns><see langword="true"/> when template and details are equal.</returns>
    public bool Equals(ErrorFrame? other)
    {
        if (other is null)
        {
            return false;
        }

        return ReferenceEquals(this, other)
            || (string.Equals(Template, other.Template, StringComparison.Ordinal) && Details.SequenceEqual(other.Details));
    }

    /// <inheritdoc />
    public override bool Equals(object? obj) => Equals(obj as ErrorFrame);

    /// <inheritdoc />
    public override int GetHashCode() => HashCodeHelper.Combine(StringComparer.Ordinal.GetHashCode(Template), Details.Count);

    /// <summary>Returns the text of the frame, including the values of sensitive details.</summary>
    /// <returns>The same text as <see cref="Message"/>.</returns>
    public override string ToString() => Message;
}
