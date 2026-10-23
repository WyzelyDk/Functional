using System.Runtime.CompilerServices;

namespace Wyzely.Functional;

/// <content>
/// <c>Try</c>: runs code that may throw and turns the exception into a failed result. This is the bridge from exception-based
/// code to results.
/// </content>
/// <remarks>
/// By default a <see cref="OperationCanceledException"/> is not turned into a result: cancellation is a request from the caller, so
/// it keeps travelling as an exception. Use <see cref="Error.Cancelled(string?)"/> explicitly when a cancellation should become a failure,
/// or change which exceptions are caught with <see cref="ResultOptions.ShouldCatch"/>.
/// </remarks>
public readonly partial struct Result
{
    /// <summary>Runs an action and returns a failed result when it throws.</summary>
    /// <param name="action">The action to run.</param>
    /// <param name="onException">
    /// Creates the error from the exception. When omitted, <see cref="Error.FromException(Exception, string?)"/> is used.
    /// </param>
    /// <returns>A successful result, or a failed result for the exception.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="action"/> is <see langword="null"/>.</exception>
    public static Result Try(Action action, Func<Exception, Error>? onException = null)
    {
        Guard.NotNull(action, nameof(action));

        try
        {
            action();

            return Success();
        }
        catch (Exception exception) when (ResultConfiguration.Current.ShouldCatch(exception))
        {
            return Failure(ErrorOf(exception, onException));
        }
    }

    /// <summary>Runs a function and returns its value, or a failed result when it throws.</summary>
    /// <typeparam name="T">The type of the value of the function.</typeparam>
    /// <param name="function">The function to run.</param>
    /// <param name="onException">
    /// Creates the error from the exception. When omitted, <see cref="Error.FromException(Exception, string?)"/> is used.
    /// </param>
    /// <returns>A successful result with the value, or a failed result for the exception.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="function"/> is <see langword="null"/>.</exception>
    public static Result<T> Try<T>(Func<T> function, Func<Exception, Error>? onException = null)
    {
        Guard.NotNull(function, nameof(function));

        try
        {
            return Success(function());
        }
        catch (Exception exception) when (ResultConfiguration.Current.ShouldCatch(exception))
        {
            return Failure<T>(ErrorOf(exception, onException));
        }
    }

    /// <summary>Runs an asynchronous action and returns a failed result when it throws.</summary>
    /// <param name="action">The asynchronous action to run.</param>
    /// <param name="onException">
    /// Creates the error from the exception. When omitted, <see cref="Error.FromException(Exception, string?)"/> is used.
    /// </param>
    /// <returns>A task that produces a successful result, or a failed result for the exception.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="action"/> is <see langword="null"/>.</exception>
    [OverloadResolutionPriority(1)]
    public static Task<Result> Try(Func<Task> action, Func<Exception, Error>? onException = null)
    {
        Guard.NotNull(action, nameof(action));

        return Core(action, onException);

        static async Task<Result> Core(Func<Task> action, Func<Exception, Error>? onException)
        {
            try
            {
                await action().ConfigureAwait(ResultConfiguration.ContinueOnCapturedContext);

                return Success();
            }
            catch (Exception exception) when (ResultConfiguration.Current.ShouldCatch(exception))
            {
                return Failure(ErrorOf(exception, onException));
            }
        }
    }

    /// <summary>Runs an asynchronous function and returns its value, or a failed result when it throws.</summary>
    /// <typeparam name="T">The type of the value of the function.</typeparam>
    /// <param name="function">The asynchronous function to run.</param>
    /// <param name="onException">
    /// Creates the error from the exception. When omitted, <see cref="Error.FromException(Exception, string?)"/> is used.
    /// </param>
    /// <returns>A task that produces a successful result with the value, or a failed result for the exception.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="function"/> is <see langword="null"/>.</exception>
    [OverloadResolutionPriority(1)]
    public static Task<Result<T>> Try<T>(Func<Task<T>> function, Func<Exception, Error>? onException = null)
    {
        Guard.NotNull(function, nameof(function));

        return Core(function, onException);

        static async Task<Result<T>> Core(Func<Task<T>> function, Func<Exception, Error>? onException)
        {
            try
            {
                return Success(await function().ConfigureAwait(ResultConfiguration.ContinueOnCapturedContext));
            }
            catch (Exception exception) when (ResultConfiguration.Current.ShouldCatch(exception))
            {
                return Failure<T>(ErrorOf(exception, onException));
            }
        }
    }

    /// <summary>Runs an action that returns a <see cref="ValueTask"/> and returns a failed result when it throws.</summary>
    /// <param name="action">The asynchronous action to run.</param>
    /// <param name="onException">
    /// Creates the error from the exception. When omitted, <see cref="Error.FromException(Exception, string?)"/> is used.
    /// </param>
    /// <returns>A value task that produces a successful result, or a failed result for the exception.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="action"/> is <see langword="null"/>.</exception>
    public static ValueTask<Result> Try(Func<ValueTask> action, Func<Exception, Error>? onException = null)
    {
        Guard.NotNull(action, nameof(action));

        return Core(action, onException);

        static async ValueTask<Result> Core(Func<ValueTask> action, Func<Exception, Error>? onException)
        {
            try
            {
                await action().ConfigureAwait(ResultConfiguration.ContinueOnCapturedContext);

                return Success();
            }
            catch (Exception exception) when (ResultConfiguration.Current.ShouldCatch(exception))
            {
                return Failure(ErrorOf(exception, onException));
            }
        }
    }

    /// <summary>Runs a function that returns a <see cref="ValueTask{TResult}"/> and returns its value, or a failed result when it throws.</summary>
    /// <typeparam name="T">The type of the value of the function.</typeparam>
    /// <param name="function">The asynchronous function to run.</param>
    /// <param name="onException">
    /// Creates the error from the exception. When omitted, <see cref="Error.FromException(Exception, string?)"/> is used.
    /// </param>
    /// <returns>A value task that produces a successful result with the value, or a failed result for the exception.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="function"/> is <see langword="null"/>.</exception>
    public static ValueTask<Result<T>> Try<T>(Func<ValueTask<T>> function, Func<Exception, Error>? onException = null)
    {
        Guard.NotNull(function, nameof(function));

        return Core(function, onException);

        static async ValueTask<Result<T>> Core(Func<ValueTask<T>> function, Func<Exception, Error>? onException)
        {
            try
            {
                return Success(await function().ConfigureAwait(ResultConfiguration.ContinueOnCapturedContext));
            }
            catch (Exception exception) when (ResultConfiguration.Current.ShouldCatch(exception))
            {
                return Failure<T>(ErrorOf(exception, onException));
            }
        }
    }

    /// <summary>
    /// Creates the error for an exception that was caught: from the handler when there is one, otherwise with the configured exception mapper.
    /// The generated <c>*Try</c> steps share it with <c>Try</c>.
    /// </summary>
    /// <param name="exception">The exception that was caught.</param>
    /// <param name="onException">The handler of the caller, or <see langword="null"/> for the default.</param>
    /// <returns>The error that describes the exception.</returns>
    internal static Error ErrorOf(Exception exception, Func<Exception, Error>? onException) =>
        onException is null ? ResultConfiguration.Current.ExceptionMapper(exception) : onException(exception);
}
