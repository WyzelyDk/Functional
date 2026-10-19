using System.Diagnostics.CodeAnalysis;

namespace Wyzely.Functional;

/// <summary>
/// Extension methods that turn the "maybe there is one" questions about sequences and dictionaries into a
/// <see cref="Maybe{T}"/> instead of an exception or a <see langword="null"/>.
/// </summary>
/// <remarks>
/// An element that is <see langword="null"/> counts as no value, like everywhere else in this library.
/// </remarks>
public static class MaybeCollectionExtensions
{
    /// <summary>Returns the first element of a sequence, or none when it is empty.</summary>
    /// <typeparam name="T">The type of the elements.</typeparam>
    /// <param name="source">The sequence to look in.</param>
    /// <returns>The first element, or none.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> is <see langword="null"/>.</exception>
    public static Maybe<T> FirstOrNone<T>(this IEnumerable<T> source)
        where T : notnull
    {
        Guard.NotNull(source, nameof(source));

        using var enumerator = source.GetEnumerator();

        return enumerator.MoveNext() ? Maybe<T>.Of(enumerator.Current) : default;
    }

    /// <summary>Returns the first element that satisfies a condition, or none when there is no such element.</summary>
    /// <typeparam name="T">The type of the elements.</typeparam>
    /// <param name="source">The sequence to look in.</param>
    /// <param name="predicate">The condition an element must satisfy.</param>
    /// <returns>The first matching element, or none.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> or <paramref name="predicate"/> is <see langword="null"/>.</exception>
    [SuppressMessage("Sonar", "S3267", Justification = "A LINQ Where would allocate an iterator on every call.")]
    public static Maybe<T> FirstOrNone<T>(this IEnumerable<T> source, Func<T, bool> predicate)
        where T : notnull
    {
        Guard.NotNull(source, nameof(source));
        Guard.NotNull(predicate, nameof(predicate));

        foreach (var item in source)
        {
            if (predicate(item))
            {
                return Maybe<T>.Of(item);
            }
        }

        return default;
    }

    /// <summary>Returns the last element of a sequence, or none when it is empty.</summary>
    /// <typeparam name="T">The type of the elements.</typeparam>
    /// <param name="source">The sequence to look in.</param>
    /// <returns>The last element, or none.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> is <see langword="null"/>.</exception>
    public static Maybe<T> LastOrNone<T>(this IEnumerable<T> source)
        where T : notnull
    {
        Guard.NotNull(source, nameof(source));

        if (source is IList<T> list)
        {
            return list.Count == 0 ? default : Maybe<T>.Of(list[list.Count - 1]);
        }

        var found = false;
        T last = default!;

        foreach (var item in source)
        {
            last = item;
            found = true;
        }

        return found ? Maybe<T>.Of(last) : default;
    }

    /// <summary>Returns the last element that satisfies a condition, or none when there is no such element.</summary>
    /// <typeparam name="T">The type of the elements.</typeparam>
    /// <param name="source">The sequence to look in.</param>
    /// <param name="predicate">The condition an element must satisfy.</param>
    /// <returns>The last matching element, or none.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> or <paramref name="predicate"/> is <see langword="null"/>.</exception>
    [SuppressMessage("Sonar", "S3267", Justification = "A LINQ Where would allocate an iterator on every call.")]
    public static Maybe<T> LastOrNone<T>(this IEnumerable<T> source, Func<T, bool> predicate)
        where T : notnull
    {
        Guard.NotNull(source, nameof(source));
        Guard.NotNull(predicate, nameof(predicate));

        var found = false;
        T last = default!;

        foreach (var item in source)
        {
            if (predicate(item))
            {
                last = item;
                found = true;
            }
        }

        return found ? Maybe<T>.Of(last) : default;
    }

    /// <summary>Returns the only element of a sequence, or none when it is empty or has more than one element.</summary>
    /// <typeparam name="T">The type of the elements.</typeparam>
    /// <param name="source">The sequence to look in.</param>
    /// <returns>The single element, or none.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> is <see langword="null"/>.</exception>
    public static Maybe<T> SingleOrNone<T>(this IEnumerable<T> source)
        where T : notnull
    {
        Guard.NotNull(source, nameof(source));

        var found = false;
        T single = default!;

        foreach (var item in source)
        {
            if (found)
            {
                return default;
            }

            single = item;
            found = true;
        }

        return found ? Maybe<T>.Of(single) : default;
    }

    /// <summary>Returns the only element that satisfies a condition, or none when there is no such element or there are several.</summary>
    /// <typeparam name="T">The type of the elements.</typeparam>
    /// <param name="source">The sequence to look in.</param>
    /// <param name="predicate">The condition an element must satisfy.</param>
    /// <returns>The single matching element, or none.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> or <paramref name="predicate"/> is <see langword="null"/>.</exception>
    public static Maybe<T> SingleOrNone<T>(this IEnumerable<T> source, Func<T, bool> predicate)
        where T : notnull
    {
        Guard.NotNull(source, nameof(source));
        Guard.NotNull(predicate, nameof(predicate));

        var found = false;
        T single = default!;

        foreach (var item in source)
        {
            if (!predicate(item))
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

    /// <summary>Returns the element at an index, or none when the index is out of range.</summary>
    /// <typeparam name="T">The type of the elements.</typeparam>
    /// <param name="source">The sequence to look in.</param>
    /// <param name="index">The zero-based index.</param>
    /// <returns>The element at the index, or none.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> is <see langword="null"/>.</exception>
    public static Maybe<T> ElementAtOrNone<T>(this IEnumerable<T> source, int index)
        where T : notnull
    {
        Guard.NotNull(source, nameof(source));

        if (index < 0)
        {
            return default;
        }

        if (source is IList<T> list)
        {
            return index < list.Count ? Maybe<T>.Of(list[index]) : default;
        }

        var position = 0;

        foreach (var item in source)
        {
            if (position == index)
            {
                return Maybe<T>.Of(item);
            }

            position++;
        }

        return default;
    }

    /// <summary>Looks a key up in a dictionary without an exception or an <c>out</c> variable.</summary>
    /// <typeparam name="TKey">The type of the keys.</typeparam>
    /// <typeparam name="TValue">The type of the values.</typeparam>
    /// <param name="source">The dictionary to look in.</param>
    /// <param name="key">The key to look for.</param>
    /// <returns>The value of the key, or none when the key is missing.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> is <see langword="null"/>.</exception>
    public static Maybe<TValue> GetValueOrNone<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue> source, TKey key)
        where TKey : notnull
        where TValue : notnull
    {
        Guard.NotNull(source, nameof(source));

        return source.TryGetValue(key, out var value) ? Maybe<TValue>.Of(value) : default;
    }

    /// <summary>Keeps the values of the maybes that have one, in order, and drops the rest.</summary>
    /// <typeparam name="T">The type of the values.</typeparam>
    /// <param name="source">The maybes to choose from.</param>
    /// <returns>The values of the somes.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> is <see langword="null"/>.</exception>
    public static IEnumerable<T> Choose<T>(this IEnumerable<Maybe<T>> source)
        where T : notnull
    {
        Guard.NotNull(source, nameof(source));

        return ChooseCore(source);

        static IEnumerable<T> ChooseCore(IEnumerable<Maybe<T>> maybes)
        {
            foreach (var maybe in maybes)
            {
                if (maybe.Unpack(out var value))
                {
                    yield return value;
                }
            }
        }
    }

    /// <summary>Maps every element to a maybe and keeps the values of the ones that have one, in order.</summary>
    /// <typeparam name="T">The type of the elements of the source.</typeparam>
    /// <typeparam name="TResult">The type of the values that the selector produces.</typeparam>
    /// <param name="source">The sequence to map.</param>
    /// <param name="selector">Maps an element to a maybe.</param>
    /// <returns>The values of the somes.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> or <paramref name="selector"/> is <see langword="null"/>.</exception>
    public static IEnumerable<TResult> Choose<T, TResult>(this IEnumerable<T> source, Func<T, Maybe<TResult>> selector)
        where TResult : notnull
    {
        Guard.NotNull(source, nameof(source));
        Guard.NotNull(selector, nameof(selector));

        return ChooseCore(source, selector);

        static IEnumerable<TResult> ChooseCore(IEnumerable<T> items, Func<T, Maybe<TResult>> map)
        {
            foreach (var item in items)
            {
                if (map(item).Unpack(out var value))
                {
                    yield return value;
                }
            }
        }
    }
}
