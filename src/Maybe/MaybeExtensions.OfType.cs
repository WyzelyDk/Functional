namespace Wyzely.Functional;

/// <content>
/// <c>OfType</c>: narrows a maybe to a maybe of a type that its value can have.
/// The form for a <see cref="Maybe{T}"/> is the instance method <see cref="Maybe{T}.OfType{TResult}"/>, which takes one type argument.
/// A task cannot have an instance method, and C# cannot give only some of the type arguments of an extension method,
/// so the forms for a task and a value task take both: <c>task.OfType&lt;Animal, Dog&gt;()</c>.
/// </content>
public static partial class MaybeExtensions
{
    /// <summary>Narrows the maybe that a task produces to a maybe of a type that its value can have.</summary>
    /// <typeparam name="T">The type of the value of the receiver maybe.</typeparam>
    /// <typeparam name="TResult">The type to narrow the value to.</typeparam>
    /// <param name="maybe">The task that produces the maybe to operate on. It must not be <see langword="null"/>.</param>
    /// <returns>A task that produces the value as a <typeparamref name="TResult"/>, or none when there is no value or it is of another type.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="maybe"/> is <see langword="null"/>.</exception>
    public static Task<Maybe<TResult>> OfType<T, TResult>(this Task<Maybe<T>> maybe)
        where T : notnull
        where TResult : notnull => maybe.Bind(static value => value is TResult result ? Maybe.Some(result) : Maybe.None<TResult>());

    /// <summary>Narrows the maybe that a value task produces to a maybe of a type that its value can have.</summary>
    /// <typeparam name="T">The type of the value of the receiver maybe.</typeparam>
    /// <typeparam name="TResult">The type to narrow the value to.</typeparam>
    /// <param name="maybe">The value task that produces the maybe to operate on.</param>
    /// <returns>A value task that produces the value as a <typeparamref name="TResult"/>, or none when there is no value or it is of another type.</returns>
    public static ValueTask<Maybe<TResult>> OfType<T, TResult>(this ValueTask<Maybe<T>> maybe)
        where T : notnull
        where TResult : notnull => maybe.Bind(static value => value is TResult result ? Maybe.Some(result) : Maybe.None<TResult>());
}
