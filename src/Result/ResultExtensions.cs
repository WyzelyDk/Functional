namespace Wyzely.Functional;

/// <summary>
/// The railway methods of <see cref="Result"/> and <see cref="Result{T}"/>: <c>Map</c>, <c>Bind</c>, <c>Tap</c>,
/// <c>TapError</c>, <c>MapError</c>, <c>Ensure</c>, <c>Check</c>, <c>Recover</c>, <c>Compensate</c>, <c>Match</c> and <c>Switch</c>.
/// </summary>
/// <remarks>
/// <para>
/// Every method exists in nine variants that share one name: the receiver can be a result, a <see cref="Task{TResult}"/> of a
/// result or a <see cref="ValueTask{TResult}"/> of a result, and the delegate can be synchronous, return a <see cref="Task"/>
/// or return a <see cref="ValueTask"/>. A chain can therefore mix synchronous and asynchronous steps freely, without
/// <c>await</c> in the middle and without an <c>Async</c> suffix:
/// </para>
/// <code>
/// var outcome = await repository.FindAsync(id)     // Task&lt;Result&lt;Order&gt;&gt;
///     .Ensure(order => order.IsOpen, Error.Conflict("The order is closed."))
///     .Bind(order => payments.ChargeAsync(order))  // Task&lt;Result&lt;Receipt&gt;&gt;
///     .Map(receipt => receipt.Number)
///     .MapError(error => error.WithContext("Checkout"));
/// </code>
/// <para>
/// The return type follows the receiver: a synchronous receiver with a synchronous delegate returns a result, an
/// asynchronous receiver keeps its kind (<see cref="Task{TResult}"/> or <see cref="ValueTask{TResult}"/>), and a synchronous
/// receiver with an asynchronous delegate returns the kind of the delegate. Awaiting inside the methods never captures the
/// synchronization context.
/// </para>
/// </remarks>
public static partial class ResultExtensions
{
}
