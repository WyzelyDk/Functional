namespace Wyzely.Functional;

/// <content>
/// Passing a failure on as a failure of another type.
/// </content>
public readonly partial struct Result<T>
{
    /// <summary>
    /// Returns the failure of this result as a failed result of another value type, with the same error. Check <see cref="IsFailure"/> first.
    /// </summary>
    /// <typeparam name="TOther">The type of the value that the other result would have held.</typeparam>
    /// <returns>A failed result that holds the error of this result.</returns>
    /// <exception cref="InvalidOperationException">The result succeeded and therefore has no error to pass on.</exception>
    public Result<TOther> ConvertFailure<TOther>()
    {
        if (_error is null)
        {
            ResultThrow.NotFailed();
        }

        return Result<TOther>.Failed(_error);
    }
}
