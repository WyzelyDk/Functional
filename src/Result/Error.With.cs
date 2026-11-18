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
    public Error WithCode(string code)
    {
        Guard.NotNullOrWhiteSpace(code, nameof(code));

        return new(Template, Kind, code, IsTransient, Exception, Details, Context, Children);
    }

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
    public Error WithDetail(string name, object? value, bool isSensitive = false)
    {
        var detail = new ErrorDetail(name, value, isSensitive);
        var details = Append(Details, detail);

        return new(Template, Kind, Code, IsTransient, Exception, details, Context, Children);
    }

    /// <summary>Returns a copy of the error with the exception that caused it.</summary>
    /// <param name="exception">The cause of the error.</param>
    /// <returns>A modified copy of the error.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="exception"/> is <see langword="null"/>.</exception>
    public Error WithException(Exception exception)
    {
        Guard.NotNull(exception, nameof(exception));

        return new(Template, Kind, Code, IsTransient, exception, Details, Context, Children);
    }

    /// <summary>Returns a copy of the error with a context frame as the new outermost frame.</summary>
    /// <param name="context">
    /// What the caller was doing, for example a chain name. It is a message template, like the message of an error,
    /// so <c>{{</c> and <c>}}</c> produce a single brace. It must not be empty.
    /// </param>
    /// <returns>
    /// A modified copy of the error. For an aggregate error the frame is added to every child, so each individual message keeps its own context.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="context"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="context"/> is empty or consists only of white space.</exception>
    public Error WithContext(string context)
    {
        Guard.NotNullOrWhiteSpace(context, nameof(context));
        var frame = new ErrorFrame(context, NoDetails);

        return WithContext(frame);
    }

    /// <summary>Returns a copy of the error with a context frame, and one detail that fills a placeholder of the frame, as the new outermost frame.</summary>
    /// <param name="context">What the caller was doing, as a message template such as <c>Checkout of order {OrderId}</c>. It must not be empty.</param>
    /// <param name="name">The name of the detail, which is matched, case-sensitively, with a placeholder of <paramref name="context"/>.</param>
    /// <param name="value">The value of the detail, which may be <see langword="null"/>.</param>
    /// <param name="isSensitive"><see langword="true"/> when the value must not appear in redacted output, for example an email address.</param>
    /// <returns>A modified copy of the error. For an aggregate error the frame is added to every child.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="context"/> or <paramref name="name"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="context"/> or <paramref name="name"/> is empty or consists only of white space.</exception>
    public Error WithContext(string context, string name, object? value, bool isSensitive = false)
    {
        Guard.NotNullOrWhiteSpace(context, nameof(context));
        var detail = new ErrorDetail(name, value, isSensitive);
        var details = new ReadOnlyCollection<ErrorDetail>([detail]);
        var frame = new ErrorFrame(context, details);

        return WithContext(frame);
    }

    /// <summary>Returns a copy of the error with a context frame and the details that fill its placeholders as the new outermost frame.</summary>
    /// <param name="context">
    /// What the caller was doing, as a message template such as <c>Checkout of {OrderId} for {Customer}</c>. It must not be empty.
    /// </param>
    /// <param name="details">The details whose names are matched, case-sensitively, with the placeholders of <paramref name="context"/>.</param>
    /// <returns>A modified copy of the error. For an aggregate error the frame is added to every child.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="context"/> or <paramref name="details"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="context"/> is empty or consists only of white space.</exception>
    public Error WithContext(string context, params ErrorDetail[] details)
    {
        Guard.NotNullOrWhiteSpace(context, nameof(context));
        Guard.NotNull(details, nameof(details));
        var frameDetails = new ReadOnlyCollection<ErrorDetail>([.. details]);
        var frame = new ErrorFrame(context, frameDetails);

        return WithContext(frame);
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

    private Error WithContext(ErrorFrame frame)
    {
        if (IsAggregate)
        {
            var children = new List<Error>(Children.Count);

            foreach (var child in Children)
            {
                var withFrame = child.WithContext(frame);
                children.Add(withFrame);
            }

            var wrapped = new ReadOnlyCollection<Error>(children);

            return new Error(Template, Kind, Code, IsTransient, Exception, Details, Context, wrapped);
        }

        var frames = Prepend(Context, frame);

        return new Error(Template, Kind, Code, IsTransient, Exception, Details, frames, Children);
    }
}
