using System.Diagnostics.CodeAnalysis;

namespace Wyzely.Functional;

/// <content>
/// Reading the outcome of a result: the error of a failure, with and without a throwing accessor.
/// </content>
public readonly partial struct Result
{
    /// <summary>Returns the error of a failed result. Check <see cref="IsFailure"/> first.</summary>
    /// <returns>The error of the failed result.</returns>
    /// <exception cref="InvalidOperationException">The result succeeded and therefore has no error.</exception>
    public Error GetError()
    {
        if (_error is null)
        {
            ResultThrow.NotFailed();
        }

        return _error;
    }

    /// <summary>Gets the error of a failed result without throwing when the result succeeded.</summary>
    /// <param name="error">The error of a failed result, or <see langword="null"/> when the result succeeded.</param>
    /// <returns><see langword="true"/> when the result failed.</returns>
    public bool TryGetError([NotNullWhen(true)] out Error? error)
    {
        error = FailureOrNull();

        return error is not null;
    }

    /// <summary>Throws when the result failed, so failures can leave the railway and become exceptions.</summary>
    /// <exception cref="ResultFailedException">The result failed. The exception carries the error.</exception>
    public void ThrowIfFailure()
    {
        var failure = FailureOrNull();

        if (failure is not null)
        {
            ResultThrow.Failed(failure);
        }
    }
}
