namespace Wyzely.Functional.Internal;

/// <summary>
/// Holds one completed task per value type for "no value", so that a task chain that ends in none hands out a shared task instead of
/// allocating a new one at every step. A completed task is immutable, so sharing it is safe. The runtime already caches the task of
/// a default value for value types without references (<c>Maybe&lt;int&gt;</c>), but not for the maybe of a reference type, which is the
/// common case.
/// </summary>
/// <typeparam name="T">The type of the value that the maybe would hold.</typeparam>
internal static class MaybeTasks<T>
    where T : notnull
{
    /// <summary>The completed task of a maybe without a value.</summary>
    internal static readonly Task<Maybe<T>> None = Task.FromResult(default(Maybe<T>));
}
