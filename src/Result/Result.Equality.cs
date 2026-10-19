namespace Wyzely.Functional;

/// <content>
/// Value equality: two successes are equal, and two failures are equal when their errors are. A <see langword="default"/> result is
/// a success, so it equals <see cref="Success()"/>.
/// </content>
public readonly partial struct Result
{
    private const int SuccessHashCode = 1;

    /// <summary>Determines whether two results are equal.</summary>
    /// <param name="left">The first result.</param>
    /// <param name="right">The second result.</param>
    /// <returns><see langword="true"/> when both succeeded or both failed with equal errors.</returns>
    public static bool operator ==(Result left, Result right) => left.Equals(right);

    /// <summary>Determines whether two results are different.</summary>
    /// <param name="left">The first result.</param>
    /// <param name="right">The second result.</param>
    /// <returns><see langword="true"/> when the results differ in state or error.</returns>
    public static bool operator !=(Result left, Result right) => !left.Equals(right);

    /// <inheritdoc />
    public bool Equals(Result other)
    {
        if (_error is null)
        {
            return other._error is null;
        }

        return _error.Equals(other._error);
    }

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is Result other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode()
        => _error is null ? SuccessHashCode : _error.GetHashCode();
}
