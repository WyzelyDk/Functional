using System.Collections.ObjectModel;

namespace Wyzely.Functional;

/// <content>
/// Combining several errors into one, so that a result only ever carries a single error.
/// </content>
public sealed partial class Error
{
    /// <summary>Combines several errors into one error whose <see cref="Children"/> are the individual errors.</summary>
    /// <param name="errors">The errors to combine. Nested aggregates are flattened.</param>
    /// <returns>
    /// The only error when exactly one individual error remains after flattening, otherwise an aggregate error. The kind of an
    /// aggregate is the kind its children share, or <see cref="ErrorKind.Failure"/> when they differ; it is transient when every child is.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="errors"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="errors"/> is empty or contains <see langword="null"/>.</exception>
    public static Error Aggregate(IEnumerable<Error> errors)
    {
        Guard.NotNull(errors, nameof(errors));
        var leaves = new List<Error>();

        foreach (var error in errors)
        {
            if (error is null)
            {
                throw new ArgumentException("The errors cannot contain null.", nameof(errors));
            }

            leaves.AddRange(error.Flatten());
        }

        return leaves.Count switch
        {
            0 => throw new ArgumentException("At least one error is required.", nameof(errors)),
            1 => leaves[0],
            _ => CreateAggregate(leaves),
        };
    }

    /// <summary>Returns the individual errors: the children of an aggregate error, or the error itself.</summary>
    /// <returns>A list with at least one error.</returns>
    public IReadOnlyList<Error> Flatten() => IsAggregate ? Children : new ReadOnlyCollection<Error>([this]);

    private static Error CreateAggregate(List<Error> leaves)
    {
        var kind = leaves[0].Kind;
        var isTransient = true;
        var sharesKind = true;

        foreach (var leaf in leaves)
        {
            sharesKind &= leaf.Kind.Equals(kind);
            isTransient &= leaf.IsTransient;
        }

        return new Error(
            ResultConfiguration.Current.AggregateMessage,
            sharesKind ? kind : default,
            null,
            isTransient,
            null,
            NoDetails,
            NoContext,
            new ReadOnlyCollection<Error>(leaves));
    }
}
