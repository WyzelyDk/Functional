using System.Collections.ObjectModel;

namespace Wyzely.Functional;

/// <content>
/// The <c>With...</c> methods. An error is immutable, so each of them returns a modified copy.
/// </content>
public sealed partial class Error
{
    /// <summary>Returns a copy of the error with a stable, machine-readable code.</summary>
    /// <param name="code">The code, for example <c>STORAGE_UNAVAILABLE</c>. It must not be empty.</param>
    /// <returns>A modified copy of the error.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="code"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="code"/> is empty or consists only of white space.</exception>
    public Error WithCode(string code) =>
        new(Template, Kind, Guard.NotNullOrWhiteSpace(code, nameof(code)), IsTransient, Exception, Details, Context, Children);

    /// <summary>Returns a copy of the error with another kind.</summary>
    /// <param name="kind">The new kind.</param>
    /// <returns>A modified copy of the error.</returns>
    public Error WithKind(ErrorKind kind) => new(Template, kind, Code, IsTransient, Exception, Details, Context, Children);

    /// <summary>Returns a copy of the error that is, or is not, marked as transient.</summary>
    /// <param name="isTransient"><see langword="true"/> when retrying later might succeed.</param>
    /// <returns>A modified copy of the error.</returns>
    public Error WithTransient(bool isTransient = true) => new(Template, Kind, Code, isTransient, Exception, Details, Context, Children);

    /// <summary>Returns a copy of the error with one more structured detail.</summary>
    /// <param name="name">The name of the detail. It fills the <c>{name}</c> placeholder of the message template.</param>
    /// <param name="value">The value of the detail, which may be <see langword="null"/>.</param>
    /// <param name="isSensitive"><see langword="true"/> to hide the value in redacted output.</param>
    /// <returns>A modified copy of the error.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="name"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="name"/> is empty or consists only of white space.</exception>
    public Error WithDetail(string name, object? value, bool isSensitive = false) =>
        new(Template, Kind, Code, IsTransient, Exception, Append(Details, new ErrorDetail(name, value, isSensitive)), Context, Children);

    /// <summary>Returns a copy of the error with the exception that caused it.</summary>
    /// <param name="exception">The cause of the error.</param>
    /// <returns>A modified copy of the error.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="exception"/> is <see langword="null"/>.</exception>
    public Error WithException(Exception exception) =>
        new(Template, Kind, Code, IsTransient, Guard.NotNull(exception, nameof(exception)), Details, Context, Children);

    /// <summary>Returns a copy of the error with a context frame as the new outermost frame.</summary>
    /// <param name="context">What the caller was doing, for example a chain name. It must not be empty.</param>
    /// <returns>
    /// A modified copy of the error. For an aggregate error the frame is added to every child, so each individual message
    /// keeps its own context.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="context"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="context"/> is empty or consists only of white space.</exception>
    public Error WithContext(string context)
    {
        Guard.NotNullOrWhiteSpace(context, nameof(context));

        if (IsAggregate)
        {
            var children = new List<Error>(Children.Count);

            foreach (var child in Children)
            {
                children.Add(child.WithContext(context));
            }

            return new Error(Template, Kind, Code, IsTransient, Exception, Details, Context, new ReadOnlyCollection<Error>(children));
        }

        return new Error(Template, Kind, Code, IsTransient, Exception, Details, Prepend(Context, context), Children);
    }

    private static ReadOnlyCollection<T> Append<T>(IReadOnlyList<T> source, T item)
    {
        var array = new T[source.Count + 1];

        for (var index = 0; index < source.Count; index++)
        {
            array[index] = source[index];
        }

        array[source.Count] = item;

        return new ReadOnlyCollection<T>(array);
    }

    private static ReadOnlyCollection<T> Prepend<T>(IReadOnlyList<T> source, T item)
    {
        var array = new T[source.Count + 1];
        array[0] = item;

        for (var index = 0; index < source.Count; index++)
        {
            array[index + 1] = source[index];
        }

        return new ReadOnlyCollection<T>(array);
    }
}
