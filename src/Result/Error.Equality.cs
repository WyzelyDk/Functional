namespace Wyzely.Functional;

/// <content>
/// Structural equality: two errors are equal when everything that describes them is equal.
/// </content>
public sealed partial class Error
{
    /// <summary>Determines whether two errors are equal.</summary>
    /// <param name="left">The first error.</param>
    /// <param name="right">The second error.</param>
    /// <returns><see langword="true"/> when both are <see langword="null"/> or structurally equal.</returns>
    public static bool operator ==(Error? left, Error? right) => left is null ? right is null : left.Equals(right);

    /// <summary>Determines whether two errors are different.</summary>
    /// <param name="left">The first error.</param>
    /// <param name="right">The second error.</param>
    /// <returns><see langword="true"/> when exactly one is <see langword="null"/> or they differ structurally.</returns>
    public static bool operator !=(Error? left, Error? right) => !(left == right);

    /// <summary>Determines whether another error is structurally equal to this one.</summary>
    /// <param name="other">The error to compare with.</param>
    /// <returns>
    /// <see langword="true"/> when template, code, kind, transient flag, details, context and children are equal and both errors
    /// refer to the same <see cref="Exception"/> instance.
    /// </returns>
    public bool Equals(Error? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return string.Equals(Template, other.Template, StringComparison.Ordinal)
            && string.Equals(Code, other.Code, StringComparison.Ordinal)
            && Kind.Equals(other.Kind)
            && IsTransient == other.IsTransient
            && ReferenceEquals(Exception, other.Exception)
            && Details.SequenceEqual(other.Details)
            && Context.SequenceEqual(other.Context, StringComparer.Ordinal)
            && Children.SequenceEqual(other.Children);
    }

    /// <inheritdoc />
    public override bool Equals(object? obj) => Equals(obj as Error);

    /// <inheritdoc />
    public override int GetHashCode() => HashCodeHelper.Combine(
        StringComparer.Ordinal.GetHashCode(Template),
        Code is null ? 0 : StringComparer.Ordinal.GetHashCode(Code),
        Kind.GetHashCode(),
        Children.Count);
}
