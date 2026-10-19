namespace Wyzely.Functional;

/// <content>
/// Value equality: two successes are equal when their values are, and two failures are equal when their errors are. A
/// <see langword="default"/> result is a success holding <c>default(T)</c>, so it equals <c>Result.Success(default(T))</c>.
/// </content>
public readonly partial struct Result<T>
{
    private const int SuccessHashSeed = 1;

    /// <summary>Determines whether two results are equal.</summary>
    /// <param name="left">The first result.</param>
    /// <param name="right">The second result.</param>
    /// <returns>
    /// <see langword="true"/> when both succeeded with equal values or both failed with equal errors.
    /// </returns>
    public static bool operator ==(Result<T> left, Result<T> right) => left.Equals(right);

    /// <summary>Determines whether two results are different.</summary>
    /// <param name="left">The first result.</param>
    /// <param name="right">The second result.</param>
    /// <returns><see langword="true"/> when the results differ in state, value or error.</returns>
    public static bool operator !=(Result<T> left, Result<T> right) => !left.Equals(right);

    /// <inheritdoc />
    public bool Equals(Result<T> other)
    {
        if (_error is null)
        {
            return other._error is null && EqualityComparer<T>.Default.Equals(_value, other._value);
        }

        return _error.Equals(other._error);
    }

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is Result<T> other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode()
    {
        if (_error is not null)
        {
            return _error.GetHashCode();
        }

        var valueHash = _value is null ? 0 : EqualityComparer<T>.Default.GetHashCode(_value);

        return HashCodeHelper.Combine(SuccessHashSeed, valueHash);
    }
}
