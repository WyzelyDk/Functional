using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Wyzely.Functional;

/// <summary>
/// Classifies an <see cref="Error"/> so callers can react to the class of a failure instead of parsing its message.
/// </summary>
/// <remarks>
/// <para>
/// The set of kinds is open: the well-known kinds below cover the common cases, and any other kind can be created with
/// <see cref="ErrorKind(string)"/>. Two kinds are equal when their names are equal (ordinal comparison).
/// </para>
/// <para>
/// <c>default(ErrorKind)</c> is <see cref="Failure"/>, so a default value is always a meaningful kind.
/// </para>
/// </remarks>
/// <param name="name">
/// The name of the kind, for example <c>RateLimited</c>. It must not be empty: <see langword="null"/> throws
/// <see cref="ArgumentNullException"/>, empty or white space throws <see cref="ArgumentException"/>.
/// </param>
[DebuggerDisplay("{Name}")]
public readonly struct ErrorKind(string name) : IEquatable<ErrorKind>
{
    private const string FailureName = "Failure";

    [SuppressMessage("Style", "IDE0032", Justification = "A default value has no name, so the property substitutes a fallback and cannot be an auto-property.")]
    private readonly string? _name = Guard.NotNullOrWhiteSpace(name, nameof(name));

    /// <summary>Gets the kind of an error that is just a failure without a more specific class. This is the default kind.</summary>
    public static ErrorKind Failure => default;

    /// <summary>Gets the kind of an error that was not anticipated, typically one created from an exception.</summary>
    public static ErrorKind Unexpected { get; } = new("Unexpected");

    /// <summary>Gets the kind of an error caused by invalid input or a violated rule.</summary>
    public static ErrorKind Validation { get; } = new("Validation");

    /// <summary>Gets the kind of an error caused by something that does not exist.</summary>
    public static ErrorKind NotFound { get; } = new("NotFound");

    /// <summary>Gets the kind of an error caused by a conflict with the current state, for example a duplicate.</summary>
    public static ErrorKind Conflict { get; } = new("Conflict");

    /// <summary>Gets the kind of an error caused by missing or invalid credentials.</summary>
    public static ErrorKind Unauthorized { get; } = new("Unauthorized");

    /// <summary>Gets the kind of an error caused by a lack of permission for an authenticated caller.</summary>
    public static ErrorKind Forbidden { get; } = new("Forbidden");

    /// <summary>Gets the kind of an error caused by a dependency that is currently unavailable.</summary>
    public static ErrorKind Unavailable { get; } = new("Unavailable");

    /// <summary>Gets the kind of an error caused by an operation that took too long.</summary>
    public static ErrorKind Timeout { get; } = new("Timeout");

    /// <summary>Gets the kind of an error caused by an operation that was cancelled.</summary>
    public static ErrorKind Cancelled { get; } = new("Cancelled");

    /// <summary>Gets the name of the kind. A default value reports <c>Failure</c>.</summary>
    public string Name => _name ?? FailureName;

    /// <summary>Determines whether two kinds are equal.</summary>
    /// <param name="left">The first kind.</param>
    /// <param name="right">The second kind.</param>
    /// <returns><see langword="true"/> when both kinds have the same name.</returns>
    public static bool operator ==(ErrorKind left, ErrorKind right) => left.Equals(right);

    /// <summary>Determines whether two kinds are different.</summary>
    /// <param name="left">The first kind.</param>
    /// <param name="right">The second kind.</param>
    /// <returns><see langword="true"/> when the kinds have different names.</returns>
    public static bool operator !=(ErrorKind left, ErrorKind right) => !left.Equals(right);

    /// <inheritdoc />
    public bool Equals(ErrorKind other) => string.Equals(Name, other.Name, StringComparison.Ordinal);

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is ErrorKind other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Name);

    /// <summary>Returns the name of the kind.</summary>
    /// <returns>The name of the kind.</returns>
    public override string ToString() => Name;
}
