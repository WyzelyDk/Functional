namespace Wyzely.Functional;

/// <content>
/// The conversion to an untyped result. A value or an <see cref="Error"/> never becomes a result implicitly: use
/// <see cref="Result.Success{T}(T)"/> and <see cref="Result.Failure{T}(Error)"/>, so that <c>return false;</c> in a method that
/// returns <c>Result&lt;bool&gt;</c> cannot be mistaken for a failure.
/// </content>
public readonly partial struct Result<T>
{
    /// <summary>Converts a typed result to an untyped one by dropping the value.</summary>
    /// <param name="result">The result to convert.</param>
    /// <returns>An untyped result in the same state.</returns>
    public static implicit operator Result(Result<T> result) => result.ToResult();

    /// <summary>Drops the value and keeps the state: a success stays a success and a failure keeps its error.</summary>
    /// <returns>An untyped result in the same state.</returns>
    public Result ToResult() => _error is null ? default : new Result(_error);
}
