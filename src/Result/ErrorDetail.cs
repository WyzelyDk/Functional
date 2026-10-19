using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace Wyzely.Functional;

/// <summary>
/// A named piece of structured data attached to an <see cref="Error"/>, for example the path of a file that could not be written.
/// </summary>
/// <remarks>
/// A detail serves two purposes: its value fills the matching <c>{Name}</c> placeholder in the message template of the
/// error, and it stays available as structured data for logging and telemetry. A detail marked as sensitive is replaced by
/// <c>***</c> in <see cref="Error.RedactedMessage"/> and <see cref="Error.ToRedactedString"/>.
/// </remarks>
/// <param name="name">
/// The name of the detail. It is matched, case-sensitively, with placeholders in the message template. Must not be <see langword="null"/>
/// (<see cref="ArgumentNullException"/>), empty or white space (<see cref="ArgumentException"/>).
/// </param>
/// <param name="value">The value of the detail, which may be <see langword="null"/>.</param>
/// <param name="isSensitive"><see langword="true"/> when the value must not appear in redacted output, for example a password.</param>
[DebuggerDisplay("{ToString(),nq}")]
public readonly struct ErrorDetail(string name, object? value, bool isSensitive = false) : IEquatable<ErrorDetail>
{
    [SuppressMessage("Style", "IDE0032", Justification = "A default value has no name, so the property substitutes a fallback and cannot be an auto-property.")]
    private readonly string? _name = Guard.NotNullOrWhiteSpace(name, nameof(name));

    /// <summary>Gets the name of the detail.</summary>
    public string Name => _name ?? string.Empty;

    /// <summary>Gets the value of the detail.</summary>
    public object? Value { get; } = value;

    /// <summary>Gets a value indicating whether the value must be hidden in redacted output.</summary>
    public bool IsSensitive { get; } = isSensitive;

    /// <summary>Determines whether two details are equal.</summary>
    /// <param name="left">The first detail.</param>
    /// <param name="right">The second detail.</param>
    /// <returns><see langword="true"/> when name, value and sensitivity are equal.</returns>
    public static bool operator ==(ErrorDetail left, ErrorDetail right) => left.Equals(right);

    /// <summary>Determines whether two details are different.</summary>
    /// <param name="left">The first detail.</param>
    /// <param name="right">The second detail.</param>
    /// <returns><see langword="true"/> when name, value or sensitivity differ.</returns>
    public static bool operator !=(ErrorDetail left, ErrorDetail right) => !left.Equals(right);

    /// <inheritdoc />
    public bool Equals(ErrorDetail other) =>
        string.Equals(Name, other.Name, StringComparison.Ordinal)
        && IsSensitive == other.IsSensitive
        && Equals(Value, other.Value);

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is ErrorDetail other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() => HashCodeHelper.Combine(
        StringComparer.Ordinal.GetHashCode(Name),
        Value?.GetHashCode() ?? 0,
        IsSensitive ? 1 : 0);

    /// <summary>Returns the detail as <c>Name=value</c>, with a sensitive value replaced by <c>***</c>.</summary>
    /// <returns>A redacted, human-readable form of the detail.</returns>
    public override string ToString() => $"{Name}={Render(redact: true)}";

    /// <summary>Renders the value of the detail as text.</summary>
    /// <param name="redact"><see langword="true"/> to replace a sensitive value by <c>***</c>.</param>
    /// <returns>The text to place in a message: the value, <c>null</c> for a missing value, or <c>***</c> when redacted.</returns>
    internal string Render(bool redact)
    {
        if (redact && IsSensitive)
        {
            return ResultConfiguration.Current.RedactedValue;
        }

        return Value is null ? "null" : Convert.ToString(Value, CultureInfo.InvariantCulture) ?? string.Empty;
    }
}
