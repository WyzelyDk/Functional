namespace Wyzely.Functional;

/// <content>
/// The factory methods that create maybes.
/// </content>
public static partial class Maybe
{
    /// <summary>Creates a maybe that holds a value.</summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="value">The value. It must not be <see langword="null"/>; use <see cref="From{T}(T)"/> when it may be.</param>
    /// <returns>A maybe that holds <paramref name="value"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/>.</exception>
    public static Maybe<T> Some<T>(T value)
        where T : notnull
    {
        if (value is null)
        {
            MaybeThrow.NullValue(nameof(value));
        }

        return new Maybe<T>(value);
    }

    /// <summary>Creates a maybe that holds no value.</summary>
    /// <typeparam name="T">The type of the value that the maybe would hold.</typeparam>
    /// <returns>A maybe without a value.</returns>
    public static Maybe<T> None<T>()
        where T : notnull => default;

    /// <summary>Creates a maybe from a reference that may be <see langword="null"/>.</summary>
    /// <typeparam name="T">The reference type of the value.</typeparam>
    /// <param name="value">The reference, or <see langword="null"/>.</param>
    /// <returns>A maybe that holds <paramref name="value"/>, or none when it is <see langword="null"/>.</returns>
    public static Maybe<T> From<T>(T? value)
        where T : class => Maybe<T>.Of(value);

    /// <summary>Creates a maybe from a nullable value.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="value">The nullable value.</param>
    /// <returns>A maybe that holds the value, or none when the nullable has no value.</returns>
    public static Maybe<T> From<T>(T? value)
        where T : struct => value.HasValue ? new Maybe<T>(value.GetValueOrDefault()) : default;

    /// <summary>Creates a maybe that holds a value only when a condition is true.</summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="condition">Whether the maybe should hold the value.</param>
    /// <param name="value">The value. It is only checked for <see langword="null"/> when <paramref name="condition"/> is true.</param>
    /// <returns>A maybe that holds <paramref name="value"/> when the condition is true, otherwise none.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="condition"/> is true and <paramref name="value"/> is <see langword="null"/>.</exception>
    public static Maybe<T> SomeIf<T>(bool condition, T value)
        where T : notnull => condition ? Some(value) : default;

    /// <summary>Creates a maybe that holds a value unless a condition is true.</summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="condition">Whether the maybe should hold no value.</param>
    /// <param name="value">The value. It is only checked for <see langword="null"/> when <paramref name="condition"/> is false.</param>
    /// <returns>None when the condition is true, otherwise a maybe that holds <paramref name="value"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="condition"/> is false and <paramref name="value"/> is <see langword="null"/>.</exception>
    public static Maybe<T> NoneIf<T>(bool condition, T value)
        where T : notnull => condition ? default : Some(value);
}
