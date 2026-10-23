namespace Wyzely.Functional.Internal;

/// <summary>
/// Holds the completed task of a successful result without a value, so that a task chain that ends in success hands out a shared task instead
/// of allocating a new one. A completed task is immutable, so sharing it is safe. The runtime caches the tasks of a few simple values, but not
/// the one of a <see cref="Result"/>.
/// </summary>
internal static class ResultTasks
{
    /// <summary>The completed task of a successful <see cref="Result"/>.</summary>
    internal static readonly Task<Result> Success = Task.FromResult(Result.Success());
}
