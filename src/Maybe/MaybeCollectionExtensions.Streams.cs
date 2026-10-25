#if NET8_0_OR_GREATER
using System.Runtime.CompilerServices;

namespace Wyzely.Functional;

/// <content>
/// The same questions for an asynchronous stream: the first, last or only element, the element at an index, and keeping the values of the maybes
/// that have one. A stream is read once, in order, and only as far as the answer needs; the reading stops when the cancellation token is cancelled.
/// A predicate may be asynchronous.
/// </content>
public static partial class MaybeCollectionExtensions
{
    /// <summary>Returns the first element of a stream, or none when it is empty.</summary>
    /// <typeparam name="T">The type of the elements.</typeparam>
    /// <param name="source">The stream to look in.</param>
    /// <param name="cancellationToken">A token that stops the reading of the stream.</param>
    /// <returns>A value task that produces the first element, or none.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> is <see langword="null"/>.</exception>
    public static ValueTask<Maybe<T>> FirstOrNone<T>(this IAsyncEnumerable<T> source, CancellationToken cancellationToken = default)
        where T : notnull
    {
        Guard.NotNull(source, nameof(source));

        return FirstCore(source, null, cancellationToken);
    }

    /// <summary>Returns the first element that satisfies a condition, or none when there is no such element.</summary>
    /// <typeparam name="T">The type of the elements.</typeparam>
    /// <param name="source">The stream to look in.</param>
    /// <param name="predicate">The condition an element must satisfy.</param>
    /// <param name="cancellationToken">A token that stops the reading of the stream.</param>
    /// <returns>A value task that produces the first matching element, or none. The stream is not read further.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> or <paramref name="predicate"/> is <see langword="null"/>.</exception>
    public static ValueTask<Maybe<T>> FirstOrNone<T>(this IAsyncEnumerable<T> source, Func<T, bool> predicate, CancellationToken cancellationToken = default)
        where T : notnull
    {
        Guard.NotNull(source, nameof(source));
        Guard.NotNull(predicate, nameof(predicate));

        return FirstCore(source, item => new ValueTask<bool>(predicate(item)), cancellationToken);
    }

    /// <summary>Returns the first element that satisfies an asynchronous condition, or none when there is no such element.</summary>
    /// <typeparam name="T">The type of the elements.</typeparam>
    /// <param name="source">The stream to look in.</param>
    /// <param name="predicate">The asynchronous condition an element must satisfy.</param>
    /// <param name="cancellationToken">A token that stops the reading of the stream.</param>
    /// <returns>A value task that produces the first matching element, or none. The stream is not read further.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> or <paramref name="predicate"/> is <see langword="null"/>.</exception>
    [OverloadResolutionPriority(1)]
    public static ValueTask<Maybe<T>> FirstOrNone<T>(
        this IAsyncEnumerable<T> source,
        Func<T, Task<bool>> predicate,
        CancellationToken cancellationToken = default)
        where T : notnull
    {
        Guard.NotNull(source, nameof(source));
        Guard.NotNull(predicate, nameof(predicate));

        return FirstCore(source, item => new ValueTask<bool>(predicate(item)), cancellationToken);
    }

    /// <summary>
    /// Returns the first element that satisfies a condition that returns a <see cref="ValueTask{TResult}"/>, or none when there is no such element.
    /// </summary>
    /// <typeparam name="T">The type of the elements.</typeparam>
    /// <param name="source">The stream to look in.</param>
    /// <param name="predicate">The asynchronous condition an element must satisfy.</param>
    /// <param name="cancellationToken">A token that stops the reading of the stream.</param>
    /// <returns>A value task that produces the first matching element, or none. The stream is not read further.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> or <paramref name="predicate"/> is <see langword="null"/>.</exception>
    public static ValueTask<Maybe<T>> FirstOrNone<T>(
        this IAsyncEnumerable<T> source,
        Func<T, ValueTask<bool>> predicate,
        CancellationToken cancellationToken = default)
        where T : notnull
    {
        Guard.NotNull(source, nameof(source));
        Guard.NotNull(predicate, nameof(predicate));

        return FirstCore(source, predicate, cancellationToken);
    }

    /// <summary>Returns the last element of a stream, or none when it is empty.</summary>
    /// <typeparam name="T">The type of the elements.</typeparam>
    /// <param name="source">The stream to look in.</param>
    /// <param name="cancellationToken">A token that stops the reading of the stream.</param>
    /// <returns>A value task that produces the last element, or none.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> is <see langword="null"/>.</exception>
    public static ValueTask<Maybe<T>> LastOrNone<T>(this IAsyncEnumerable<T> source, CancellationToken cancellationToken = default)
        where T : notnull
    {
        Guard.NotNull(source, nameof(source));

        return LastCore(source, null, cancellationToken);
    }

    /// <summary>Returns the last element that satisfies a condition, or none when there is no such element.</summary>
    /// <typeparam name="T">The type of the elements.</typeparam>
    /// <param name="source">The stream to look in.</param>
    /// <param name="predicate">The condition an element must satisfy.</param>
    /// <param name="cancellationToken">A token that stops the reading of the stream.</param>
    /// <returns>A value task that produces the last matching element, or none.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> or <paramref name="predicate"/> is <see langword="null"/>.</exception>
    public static ValueTask<Maybe<T>> LastOrNone<T>(this IAsyncEnumerable<T> source, Func<T, bool> predicate, CancellationToken cancellationToken = default)
        where T : notnull
    {
        Guard.NotNull(source, nameof(source));
        Guard.NotNull(predicate, nameof(predicate));

        return LastCore(source, item => new ValueTask<bool>(predicate(item)), cancellationToken);
    }

    /// <summary>Returns the last element that satisfies an asynchronous condition, or none when there is no such element.</summary>
    /// <typeparam name="T">The type of the elements.</typeparam>
    /// <param name="source">The stream to look in.</param>
    /// <param name="predicate">The asynchronous condition an element must satisfy.</param>
    /// <param name="cancellationToken">A token that stops the reading of the stream.</param>
    /// <returns>A value task that produces the last matching element, or none.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> or <paramref name="predicate"/> is <see langword="null"/>.</exception>
    [OverloadResolutionPriority(1)]
    public static ValueTask<Maybe<T>> LastOrNone<T>(
        this IAsyncEnumerable<T> source,
        Func<T, Task<bool>> predicate,
        CancellationToken cancellationToken = default)
        where T : notnull
    {
        Guard.NotNull(source, nameof(source));
        Guard.NotNull(predicate, nameof(predicate));

        return LastCore(source, item => new ValueTask<bool>(predicate(item)), cancellationToken);
    }

    /// <summary>
    /// Returns the last element that satisfies a condition that returns a <see cref="ValueTask{TResult}"/>, or none when there is no such element.
    /// </summary>
    /// <typeparam name="T">The type of the elements.</typeparam>
    /// <param name="source">The stream to look in.</param>
    /// <param name="predicate">The asynchronous condition an element must satisfy.</param>
    /// <param name="cancellationToken">A token that stops the reading of the stream.</param>
    /// <returns>A value task that produces the last matching element, or none.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> or <paramref name="predicate"/> is <see langword="null"/>.</exception>
    public static ValueTask<Maybe<T>> LastOrNone<T>(
        this IAsyncEnumerable<T> source,
        Func<T, ValueTask<bool>> predicate,
        CancellationToken cancellationToken = default)
        where T : notnull
    {
        Guard.NotNull(source, nameof(source));
        Guard.NotNull(predicate, nameof(predicate));

        return LastCore(source, predicate, cancellationToken);
    }

    /// <summary>Returns the only element of a stream, or none when it is empty or has more than one element.</summary>
    /// <typeparam name="T">The type of the elements.</typeparam>
    /// <param name="source">The stream to look in.</param>
    /// <param name="cancellationToken">A token that stops the reading of the stream.</param>
    /// <returns>A value task that produces the single element, or none. The stream is not read further once there are two.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> is <see langword="null"/>.</exception>
    public static ValueTask<Maybe<T>> SingleOrNone<T>(this IAsyncEnumerable<T> source, CancellationToken cancellationToken = default)
        where T : notnull
    {
        Guard.NotNull(source, nameof(source));

        return SingleCore(source, null, cancellationToken);
    }

    /// <summary>Returns the only element that satisfies a condition, or none when there is no such element or there are several.</summary>
    /// <typeparam name="T">The type of the elements.</typeparam>
    /// <param name="source">The stream to look in.</param>
    /// <param name="predicate">The condition an element must satisfy.</param>
    /// <param name="cancellationToken">A token that stops the reading of the stream.</param>
    /// <returns>A value task that produces the single matching element, or none. The stream is not read further once there are two.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> or <paramref name="predicate"/> is <see langword="null"/>.</exception>
    public static ValueTask<Maybe<T>> SingleOrNone<T>(this IAsyncEnumerable<T> source, Func<T, bool> predicate, CancellationToken cancellationToken = default)
        where T : notnull
    {
        Guard.NotNull(source, nameof(source));
        Guard.NotNull(predicate, nameof(predicate));

        return SingleCore(source, item => new ValueTask<bool>(predicate(item)), cancellationToken);
    }

    /// <summary>Returns the only element that satisfies an asynchronous condition, or none when there is no such element or there are several.</summary>
    /// <typeparam name="T">The type of the elements.</typeparam>
    /// <param name="source">The stream to look in.</param>
    /// <param name="predicate">The asynchronous condition an element must satisfy.</param>
    /// <param name="cancellationToken">A token that stops the reading of the stream.</param>
    /// <returns>A value task that produces the single matching element, or none. The stream is not read further once there are two.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> or <paramref name="predicate"/> is <see langword="null"/>.</exception>
    [OverloadResolutionPriority(1)]
    public static ValueTask<Maybe<T>> SingleOrNone<T>(
        this IAsyncEnumerable<T> source,
        Func<T, Task<bool>> predicate,
        CancellationToken cancellationToken = default)
        where T : notnull
    {
        Guard.NotNull(source, nameof(source));
        Guard.NotNull(predicate, nameof(predicate));

        return SingleCore(source, item => new ValueTask<bool>(predicate(item)), cancellationToken);
    }

    /// <summary>
    /// Returns the only element that satisfies a condition that returns a <see cref="ValueTask{TResult}"/>, or none when there is no such element
    /// or there are several.
    /// </summary>
    /// <typeparam name="T">The type of the elements.</typeparam>
    /// <param name="source">The stream to look in.</param>
    /// <param name="predicate">The asynchronous condition an element must satisfy.</param>
    /// <param name="cancellationToken">A token that stops the reading of the stream.</param>
    /// <returns>A value task that produces the single matching element, or none. The stream is not read further once there are two.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> or <paramref name="predicate"/> is <see langword="null"/>.</exception>
    public static ValueTask<Maybe<T>> SingleOrNone<T>(
        this IAsyncEnumerable<T> source,
        Func<T, ValueTask<bool>> predicate,
        CancellationToken cancellationToken = default)
        where T : notnull
    {
        Guard.NotNull(source, nameof(source));
        Guard.NotNull(predicate, nameof(predicate));

        return SingleCore(source, predicate, cancellationToken);
    }

    /// <summary>Returns the element at an index, or none when the index is out of range.</summary>
    /// <typeparam name="T">The type of the elements.</typeparam>
    /// <param name="source">The stream to look in.</param>
    /// <param name="index">The zero-based index.</param>
    /// <param name="cancellationToken">A token that stops the reading of the stream.</param>
    /// <returns>A value task that produces the element at the index, or none. The stream is not read beyond the index.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> is <see langword="null"/>.</exception>
    public static ValueTask<Maybe<T>> ElementAtOrNone<T>(this IAsyncEnumerable<T> source, int index, CancellationToken cancellationToken = default)
        where T : notnull
    {
        Guard.NotNull(source, nameof(source));

        return ElementAtCore(source, index, cancellationToken);
    }

    /// <summary>Keeps the values of the maybes of a stream that have one, in order, and drops the rest.</summary>
    /// <typeparam name="T">The type of the values.</typeparam>
    /// <param name="source">The stream of maybes to choose from.</param>
    /// <returns>A stream of the values of the somes.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> is <see langword="null"/>.</exception>
    public static IAsyncEnumerable<T> Choose<T>(this IAsyncEnumerable<Maybe<T>> source)
        where T : notnull
    {
        Guard.NotNull(source, nameof(source));

        return ChooseCore(source, static item => new ValueTask<Maybe<T>>(item));
    }

    /// <summary>Maps every element of a stream to a maybe and keeps the values of the ones that have one, in order.</summary>
    /// <typeparam name="T">The type of the elements of the source.</typeparam>
    /// <typeparam name="TResult">The type of the values that the selector produces.</typeparam>
    /// <param name="source">The stream to map.</param>
    /// <param name="selector">Maps an element to a maybe.</param>
    /// <returns>A stream of the values of the somes.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> or <paramref name="selector"/> is <see langword="null"/>.</exception>
    public static IAsyncEnumerable<TResult> Choose<T, TResult>(this IAsyncEnumerable<T> source, Func<T, Maybe<TResult>> selector)
        where TResult : notnull
    {
        Guard.NotNull(source, nameof(source));
        Guard.NotNull(selector, nameof(selector));

        return ChooseCore(source, item => new ValueTask<Maybe<TResult>>(selector(item)));
    }

    /// <summary>Maps every element of a stream to a maybe with an asynchronous selector and keeps the values of the ones that have one, in order.</summary>
    /// <typeparam name="T">The type of the elements of the source.</typeparam>
    /// <typeparam name="TResult">The type of the values that the selector produces.</typeparam>
    /// <param name="source">The stream to map.</param>
    /// <param name="selector">Maps an element to a maybe. It is awaited before the next element is read.</param>
    /// <returns>A stream of the values of the somes.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> or <paramref name="selector"/> is <see langword="null"/>.</exception>
    [OverloadResolutionPriority(1)]
    public static IAsyncEnumerable<TResult> Choose<T, TResult>(this IAsyncEnumerable<T> source, Func<T, Task<Maybe<TResult>>> selector)
        where TResult : notnull
    {
        Guard.NotNull(source, nameof(source));
        Guard.NotNull(selector, nameof(selector));

        return ChooseCore(source, item => new ValueTask<Maybe<TResult>>(selector(item)));
    }

    /// <summary>
    /// Maps every element of a stream to a maybe with a selector that returns a <see cref="ValueTask{TResult}"/> and keeps the values of the ones that
    /// have one.
    /// </summary>
    /// <typeparam name="T">The type of the elements of the source.</typeparam>
    /// <typeparam name="TResult">The type of the values that the selector produces.</typeparam>
    /// <param name="source">The stream to map.</param>
    /// <param name="selector">Maps an element to a maybe. It is awaited before the next element is read.</param>
    /// <returns>A stream of the values of the somes.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> or <paramref name="selector"/> is <see langword="null"/>.</exception>
    public static IAsyncEnumerable<TResult> Choose<T, TResult>(this IAsyncEnumerable<T> source, Func<T, ValueTask<Maybe<TResult>>> selector)
        where TResult : notnull
    {
        Guard.NotNull(source, nameof(source));
        Guard.NotNull(selector, nameof(selector));

        return ChooseCore(source, selector);
    }

    /// <summary>The stream behind <c>Choose</c>: the values of the somes that the selector returns, in order.</summary>
    /// <typeparam name="T">The type of the elements of the source.</typeparam>
    /// <typeparam name="TResult">The type of the values.</typeparam>
    /// <param name="source">The stream to read.</param>
    /// <param name="selector">The operation to run for an element.</param>
    /// <param name="cancellationToken">
    /// A token of the stream itself. <c>Choose</c> passes none; the token that a consumer gives to <c>WithCancellation</c> is combined with it.
    /// </param>
    /// <returns>The stream of values.</returns>
    internal static async IAsyncEnumerable<TResult> ChooseCore<T, TResult>(
        IAsyncEnumerable<T> source,
        Func<T, ValueTask<Maybe<TResult>>> selector,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
        where TResult : notnull
    {
        await foreach (var item in source.WithCancellation(cancellationToken).ConfigureAwait(MaybeConfiguration.ContinueOnCapturedContext))
        {
            var maybe = await selector(item).ConfigureAwait(MaybeConfiguration.ContinueOnCapturedContext);

            if (maybe.Unpack(out var value))
            {
                yield return value;
            }
        }
    }

    private static async ValueTask<Maybe<T>> FirstCore<T>(IAsyncEnumerable<T> source, Func<T, ValueTask<bool>>? predicate, CancellationToken cancellationToken)
        where T : notnull
    {
        await foreach (var item in source.WithCancellation(cancellationToken).ConfigureAwait(MaybeConfiguration.ContinueOnCapturedContext))
        {
            if (predicate is null || await predicate(item).ConfigureAwait(MaybeConfiguration.ContinueOnCapturedContext))
            {
                return Maybe<T>.Of(item);
            }
        }

        return default;
    }

    private static async ValueTask<Maybe<T>> LastCore<T>(IAsyncEnumerable<T> source, Func<T, ValueTask<bool>>? predicate, CancellationToken cancellationToken)
        where T : notnull
    {
        var found = false;
        T last = default!;

        await foreach (var item in source.WithCancellation(cancellationToken).ConfigureAwait(MaybeConfiguration.ContinueOnCapturedContext))
        {
            if (predicate is null || await predicate(item).ConfigureAwait(MaybeConfiguration.ContinueOnCapturedContext))
            {
                last = item;
                found = true;
            }
        }

        return found ? Maybe<T>.Of(last) : default;
    }

    private static async ValueTask<Maybe<T>> SingleCore<T>(IAsyncEnumerable<T> source, Func<T, ValueTask<bool>>? predicate, CancellationToken cancellationToken)
        where T : notnull
    {
        var found = false;
        T single = default!;

        await foreach (var item in source.WithCancellation(cancellationToken).ConfigureAwait(MaybeConfiguration.ContinueOnCapturedContext))
        {
            if (predicate is not null && !await predicate(item).ConfigureAwait(MaybeConfiguration.ContinueOnCapturedContext))
            {
                continue;
            }

            if (found)
            {
                return default;
            }

            single = item;
            found = true;
        }

        return found ? Maybe<T>.Of(single) : default;
    }

    private static async ValueTask<Maybe<T>> ElementAtCore<T>(IAsyncEnumerable<T> source, int index, CancellationToken cancellationToken)
        where T : notnull
    {
        if (index < 0)
        {
            return default;
        }

        var position = 0;

        await foreach (var item in source.WithCancellation(cancellationToken).ConfigureAwait(MaybeConfiguration.ContinueOnCapturedContext))
        {
            if (position == index)
            {
                return Maybe<T>.Of(item);
            }

            position++;
        }

        return default;
    }
}
#endif
