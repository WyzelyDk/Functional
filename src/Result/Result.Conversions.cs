namespace Wyzely.Functional;

/// <content>
/// The implicit conversion from an <see cref="Error"/>: it converts to a failure that carries it.
/// </content>
public readonly partial struct Result
{
    /// <summary>Converts an error to a failed result that carries it.</summary>
    /// <param name="error">The error of the failure.</param>
    /// <returns>A failed result with <paramref name="error"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="error"/> is <see langword="null"/>.</exception>
    public static implicit operator Result(Error error) => Failure(error);
}
