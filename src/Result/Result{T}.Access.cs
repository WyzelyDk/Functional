using System.Diagnostics.CodeAnalysis;

namespace Wyzely.Functional;

/// <content>
/// Reading the outcome of a result: the value of a success and the error of a failure, with and without throwing accessors.
/// </content>
public readonly partial struct Result<T>
{
    /// <summary>Returns the value of a successful result. Check <see cref="IsSuccess"/> first.</summary>
    /// <returns>The value of the successful result.</returns>
    /// <exception cref="ResultFailedException">The result failed. The exception carries the error.</exception>
    public T GetValue()
    {
        if (_error is not null)
        {
            ResultThrow.Failed(_error);
        }

        return _value;
    }

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

    /// <summary>Gets the value of a successful result without throwing when the result failed.</summary>
    /// <param name="value">The value of a successful result, or <see langword="default"/> when the result failed.</param>
    /// <returns><see langword="true"/> when the result succeeded.</returns>
    public bool TryGetValue([MaybeNullWhen(false)] out T value)
    {
        var failure = Unpack(out value);

        return failure is null;
    }

    /// <summary>Gets the error of a failed result without throwing when the result succeeded.</summary>
    /// <param name="error">The error of a failed result, or <see langword="null"/> when the result succeeded.</param>
    /// <returns><see langword="true"/> when the result failed.</returns>
    public bool TryGetError([NotNullWhen(true)] out Error? error)
    {
        error = Unpack(out _);

        return error is not null;
    }

    /// <summary>Returns the value of a successful result, or a fallback when the result failed.</summary>
    /// <param name="fallback">The value to return for a failed result.</param>
    /// <returns>The value, or <paramref name="fallback"/>.</returns>
    public T ValueOr(T fallback)
    {
        var failure = Unpack(out var value);

        return failure is null ? value : fallback;
    }

    /// <summary>Splits the result into its value and its error, for use with deconstruction: <c>var (value, error) = result;</c>.</summary>
    /// <param name="value">The value of a successful result, or <see langword="default"/> when the result failed.</param>
    /// <param name="error">The error of a failed result, or <see langword="null"/> when the result succeeded.</param>
    public void Deconstruct(out T? value, out Error? error)
    {
        error = Unpack(out var unpacked);
        value = unpacked;
    }
}
