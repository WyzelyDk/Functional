using System.Runtime.CompilerServices;

namespace Wyzely.Functional;

/// <content>
/// <c>Try</c> for one kind of exception: code that may throw an exception of a named type, which becomes a failure, while every other
/// exception keeps travelling.
/// </content>
/// <remarks>
/// <para>
/// The exception type is the type of the parameter of the handler, so write <c>(JsonException exception) =&gt; Error.FromException(exception)</c>
/// and both the type of the value and the type of the exception are inferred. An exception of that type, or of a type derived from it, is caught
/// when the optional <c>when</c> condition also holds.
/// </para>
/// <para>
/// Naming the type is the decision, so <see cref="ResultOptions.ShouldCatch"/> is not consulted: this is also how a cancellation is caught on
/// purpose, for example <c>(OperationCanceledException exception) =&gt; ...</c> with <c>when: _ =&gt; !cancellationToken.IsCancellationRequested</c>
/// to turn a timeout, but not a cancellation by the caller, into a failure. A handler that takes an <see cref="Exception"/> is the
/// <c>Try</c> without a type, which does follow the setting.
/// </para>
/// </remarks>
public readonly partial struct Result
{
    /// <summary>Runs an action and returns a failed result when it throws an exception of the named type.</summary>
    /// <typeparam name="TException">The type of the exception to catch, which is the type of the parameter of <paramref name="onException"/>.</typeparam>
    /// <param name="action">The action to run.</param>
    /// <param name="onException">Creates the error from the exception.</param>
    /// <param name="when">A condition that the exception must also meet to be caught; an exception that does not meet it keeps travelling.</param>
    /// <returns>A successful result, or a failed result with the error for the exception.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="action"/> or <paramref name="onException"/> is <see langword="null"/>.</exception>
    public static Result Try<TException>(Action action, Func<TException, Error> onException, Func<TException, bool>? when = null)
        where TException : Exception
    {
        Guard.NotNull(action, nameof(action));
        Guard.NotNull(onException, nameof(onException));

        try
        {
            action();

            return Success();
        }
        catch (TException exception) when (Matches(when, exception))
        {
            return Failure(onException(exception));
        }
    }

    /// <summary>Runs a function and returns its value, or a failed result when it throws an exception of the named type.</summary>
    /// <typeparam name="T">The type of the value of the function.</typeparam>
    /// <typeparam name="TException">The type of the exception to catch, which is the type of the parameter of <paramref name="onException"/>.</typeparam>
    /// <param name="function">The function to run.</param>
    /// <param name="onException">Creates the error from the exception.</param>
    /// <param name="when">A condition that the exception must also meet to be caught; an exception that does not meet it keeps travelling.</param>
    /// <returns>A successful result with the value, or a failed result with the error for the exception.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="function"/> or <paramref name="onException"/> is <see langword="null"/>.</exception>
    public static Result<T> Try<T, TException>(Func<T> function, Func<TException, Error> onException, Func<TException, bool>? when = null)
        where TException : Exception
    {
        Guard.NotNull(function, nameof(function));
        Guard.NotNull(onException, nameof(onException));

        try
        {
            return Success(function());
        }
        catch (TException exception) when (Matches(when, exception))
        {
            return Failure<T>(onException(exception));
        }
    }

    /// <summary>Runs an asynchronous action and returns a failed result when it throws an exception of the named type.</summary>
    /// <typeparam name="TException">The type of the exception to catch, which is the type of the parameter of <paramref name="onException"/>.</typeparam>
    /// <param name="action">The asynchronous action to run.</param>
    /// <param name="onException">Creates the error from the exception.</param>
    /// <param name="when">A condition that the exception must also meet to be caught; an exception that does not meet it keeps travelling.</param>
    /// <returns>A task that produces a successful result, or a failed result with the error for the exception.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="action"/> or <paramref name="onException"/> is <see langword="null"/>.</exception>
    [OverloadResolutionPriority(1)]
    public static Task<Result> Try<TException>(Func<Task> action, Func<TException, Error> onException, Func<TException, bool>? when = null)
        where TException : Exception
    {
        Guard.NotNull(action, nameof(action));
        Guard.NotNull(onException, nameof(onException));

        return Core(action, onException, when);

        static async Task<Result> Core(Func<Task> action, Func<TException, Error> onException, Func<TException, bool>? when)
        {
            try
            {
                await action().ConfigureAwait(ResultConfiguration.ContinueOnCapturedContext);

                return Success();
            }
            catch (TException exception) when (Matches(when, exception))
            {
                return Failure(onException(exception));
            }
        }
    }

    /// <summary>Runs an asynchronous function and returns its value, or a failed result when it throws an exception of the named type.</summary>
    /// <typeparam name="T">The type of the value of the function.</typeparam>
    /// <typeparam name="TException">The type of the exception to catch, which is the type of the parameter of <paramref name="onException"/>.</typeparam>
    /// <param name="function">The asynchronous function to run.</param>
    /// <param name="onException">Creates the error from the exception.</param>
    /// <param name="when">A condition that the exception must also meet to be caught; an exception that does not meet it keeps travelling.</param>
    /// <returns>A task that produces a successful result with the value, or a failed result with the error for the exception.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="function"/> or <paramref name="onException"/> is <see langword="null"/>.</exception>
    [OverloadResolutionPriority(1)]
    public static Task<Result<T>> Try<T, TException>(Func<Task<T>> function, Func<TException, Error> onException, Func<TException, bool>? when = null)
        where TException : Exception
    {
        Guard.NotNull(function, nameof(function));
        Guard.NotNull(onException, nameof(onException));

        return Core(function, onException, when);

        static async Task<Result<T>> Core(Func<Task<T>> function, Func<TException, Error> onException, Func<TException, bool>? when)
        {
            try
            {
                return Success(await function().ConfigureAwait(ResultConfiguration.ContinueOnCapturedContext));
            }
            catch (TException exception) when (Matches(when, exception))
            {
                return Failure<T>(onException(exception));
            }
        }
    }

    /// <summary>Runs an action that returns a <see cref="ValueTask"/> and returns a failed result when it throws an exception of the named type.</summary>
    /// <typeparam name="TException">The type of the exception to catch, which is the type of the parameter of <paramref name="onException"/>.</typeparam>
    /// <param name="action">The asynchronous action to run.</param>
    /// <param name="onException">Creates the error from the exception.</param>
    /// <param name="when">A condition that the exception must also meet to be caught; an exception that does not meet it keeps travelling.</param>
    /// <returns>A value task that produces a successful result, or a failed result with the error for the exception.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="action"/> or <paramref name="onException"/> is <see langword="null"/>.</exception>
    public static ValueTask<Result> Try<TException>(Func<ValueTask> action, Func<TException, Error> onException, Func<TException, bool>? when = null)
        where TException : Exception
    {
        Guard.NotNull(action, nameof(action));
        Guard.NotNull(onException, nameof(onException));

        return Core(action, onException, when);

        static async ValueTask<Result> Core(Func<ValueTask> action, Func<TException, Error> onException, Func<TException, bool>? when)
        {
            try
            {
                await action().ConfigureAwait(ResultConfiguration.ContinueOnCapturedContext);

                return Success();
            }
            catch (TException exception) when (Matches(when, exception))
            {
                return Failure(onException(exception));
            }
        }
    }

    /// <summary>
    /// Runs a function that returns a <see cref="ValueTask{TResult}"/> and returns its value, or a failed result when it throws an exception of
    /// the named type.
    /// </summary>
    /// <typeparam name="T">The type of the value of the function.</typeparam>
    /// <typeparam name="TException">The type of the exception to catch, which is the type of the parameter of <paramref name="onException"/>.</typeparam>
    /// <param name="function">The asynchronous function to run.</param>
    /// <param name="onException">Creates the error from the exception.</param>
    /// <param name="when">A condition that the exception must also meet to be caught; an exception that does not meet it keeps travelling.</param>
    /// <returns>A value task that produces a successful result with the value, or a failed result with the error for the exception.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="function"/> or <paramref name="onException"/> is <see langword="null"/>.</exception>
    public static ValueTask<Result<T>> Try<T, TException>(Func<ValueTask<T>> function, Func<TException, Error> onException, Func<TException, bool>? when = null)
        where TException : Exception
    {
        Guard.NotNull(function, nameof(function));
        Guard.NotNull(onException, nameof(onException));

        return Core(function, onException, when);

        static async ValueTask<Result<T>> Core(Func<ValueTask<T>> function, Func<TException, Error> onException, Func<TException, bool>? when)
        {
            try
            {
                return Success(await function().ConfigureAwait(ResultConfiguration.ContinueOnCapturedContext));
            }
            catch (TException exception) when (Matches(when, exception))
            {
                return Failure<T>(onException(exception));
            }
        }
    }

    private static bool Matches<TException>(Func<TException, bool>? when, TException exception)
        where TException : Exception => when is null || when(exception);
}
