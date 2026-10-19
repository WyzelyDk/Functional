namespace Wyzely.Functional;

/// <summary>
/// The methods that connect <see cref="Maybe{T}"/> and <see cref="Result{T}"/>: <c>ToResult</c> turns an absent value into a failure that
/// you describe, and <c>ToMaybe</c> turns a failure into an absent value.
/// </summary>
/// <remarks>
/// <para>
/// Both exist for a plain receiver, a <see cref="Task{TResult}"/> and a <see cref="ValueTask{TResult}"/>, so a chain can move between the two
/// types without <c>await</c> in the middle:
/// </para>
/// <code>
/// var outcome = await repository.FindAsync(id)        // Task&lt;Maybe&lt;Customer&gt;&gt;
///     .ToResult(Error.NotFound("No such customer."))  // Task&lt;Result&lt;Customer&gt;&gt;
///     .Bind(customer => billing.ChargeAsync(customer));
/// </code>
/// <para>
/// Moving from a result to a maybe loses the error on purpose: <c>ToMaybe</c> is for the place where only "is there a value" matters. The
/// asynchronous variants await without capturing the synchronization context unless the settings of the two packages say otherwise.
/// </para>
/// </remarks>
public static partial class MaybeResultExtensions
{
}
