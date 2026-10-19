namespace Wyzely.Functional;

/// <content>
/// <c>Flatten</c>: removes one level of nesting from a maybe of a maybe.
/// </content>
public static partial class MaybeExtensions
{
    /// <summary>Removes one level of nesting from a maybe of a maybe. This is <c>Bind</c> with the identity function.</summary>
    /// <typeparam name="T">The type of the inner value.</typeparam>
    /// <param name="maybe">The nested maybe.</param>
    /// <returns>The inner maybe, or none when the outer maybe has no value.</returns>
    public static Maybe<T> Flatten<T>(this Maybe<Maybe<T>> maybe)
        where T : notnull => maybe.Bind(static inner => inner);

    /// <summary>Removes one level of nesting from the maybe of a maybe that a task produces.</summary>
    /// <typeparam name="T">The type of the inner value.</typeparam>
    /// <param name="maybe">The task that produces the nested maybe. It must not be <see langword="null"/>.</param>
    /// <returns>A task that produces the inner maybe, or none when the outer maybe has no value.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="maybe"/> is <see langword="null"/>.</exception>
    public static Task<Maybe<T>> Flatten<T>(this Task<Maybe<Maybe<T>>> maybe)
        where T : notnull => maybe.Bind(static inner => inner);

    /// <summary>Removes one level of nesting from the maybe of a maybe that a value task produces.</summary>
    /// <typeparam name="T">The type of the inner value.</typeparam>
    /// <param name="maybe">The value task that produces the nested maybe.</param>
    /// <returns>A value task that produces the inner maybe, or none when the outer maybe has no value.</returns>
    public static ValueTask<Maybe<T>> Flatten<T>(this ValueTask<Maybe<Maybe<T>>> maybe)
        where T : notnull => maybe.Bind(static inner => inner);
}
