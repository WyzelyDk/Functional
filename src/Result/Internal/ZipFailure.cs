namespace Wyzely.Functional.Internal;

/// <summary>
/// Builds the failed result that the generated <c>Zip</c> methods return: the error of the one result that failed, or an aggregate of all
/// the errors.
/// </summary>
internal static class ZipFailure
{
    /// <summary>Creates a failed result from the errors of the results that were zipped.</summary>
    /// <typeparam name="T">The type of the value of the failed result.</typeparam>
    /// <param name="errors">
    /// The error of each result in argument order, with <see langword="null"/> for the ones that succeeded. At least one must be an error.
    /// </param>
    /// <returns>A failed result with the single error, or with an aggregate of every error in argument order.</returns>
    internal static Result<T> Of<T>(params Error?[] errors)
    {
        var failures = errors.OfType<Error>().ToList();

        return Result<T>.Failed(failures.Count == 1 ? failures[0] : Error.Aggregate(failures));
    }
}
