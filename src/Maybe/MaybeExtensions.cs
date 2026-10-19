namespace Wyzely.Functional;

/// <summary>
/// The railway methods of <see cref="Maybe{T}"/>: <c>Map</c>, <c>Bind</c>, <c>Filter</c>, <c>Tap</c>, <c>TapNone</c>,
/// <c>Or</c>, <c>ValueOr</c>, <c>Match</c> and <c>Switch</c>.
/// </summary>
/// <remarks>
/// <para>
/// Every method exists in nine variants that share one name: the receiver can be a maybe, a <see cref="Task{TResult}"/> of a
/// maybe or a <see cref="ValueTask{TResult}"/> of a maybe, and the delegate can be synchronous, return a <see cref="Task"/>
/// or return a <see cref="ValueTask"/>. A chain can therefore mix synchronous and asynchronous steps freely, without
/// <c>await</c> in the middle and without an <c>Async</c> suffix:
/// </para>
/// <code>
/// var name = await repository.FindAsync(id)      // Task&lt;Maybe&lt;User&gt;&gt;
///     .Filter(user => user.IsActive)
///     .Bind(user => directory.LookupAsync(user))  // Task&lt;Maybe&lt;Person&gt;&gt;
///     .Map(person => person.Name)
///     .ValueOr(() => "unknown");
/// </code>
/// <para>
/// The return type follows the receiver: a synchronous receiver with a synchronous delegate returns a maybe, an
/// asynchronous receiver keeps its kind (<see cref="Task{TResult}"/> or <see cref="ValueTask{TResult}"/>), and a synchronous
/// receiver with an asynchronous delegate returns the kind of the delegate. Awaiting inside the methods never captures the
/// synchronization context.
/// </para>
/// </remarks>
public static partial class MaybeExtensions
{
}
