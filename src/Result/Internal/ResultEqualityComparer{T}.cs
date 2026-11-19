namespace Wyzely.Functional.Internal;

/// <summary>Compares results: two successes by the given comparer of the values, two failures by their errors.</summary>
/// <typeparam name="T">The type of the values.</typeparam>
/// <param name="valueComparer">Compares and hashes the values of successes.</param>
internal sealed class ResultEqualityComparer<T>(IEqualityComparer<T> valueComparer) : IEqualityComparer<Result<T>>
{
    /// <summary>Determines whether two results are equal under the comparer of the values.</summary>
    /// <param name="x">The first result.</param>
    /// <param name="y">The second result.</param>
    /// <returns><see langword="true"/> when both succeeded with values that the comparer finds equal, or both failed with equal errors.</returns>
    public bool Equals(Result<T> x, Result<T> y) => x.Equals(y, valueComparer);

    /// <summary>Gets the hash code of a result under the comparer of the values.</summary>
    /// <param name="obj">The result.</param>
    /// <returns>The hash code.</returns>
    public int GetHashCode(Result<T> obj) => obj.GetHashCode(valueComparer);
}
