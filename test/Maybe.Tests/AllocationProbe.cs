namespace Wyzely.Functional.MaybeTests;

/// <summary>Measures what a piece of code allocates on the current thread.</summary>
internal static class AllocationProbe
{
    private const int Rounds = 5;

    /// <summary>
    /// Runs the action in several rounds and returns the fewest bytes that any round allocated. Code that allocates does so in every round, while
    /// a one-off allocation of the runtime (the JIT, a type that loads late) shows in one round only, so the smallest round is the code's own.
    /// </summary>
    /// <param name="action">The code to measure. It is run after the caller has warmed it up.</param>
    /// <returns>The smallest number of bytes allocated by one round.</returns>
    internal static long Smallest(Action action)
    {
        var smallest = long.MaxValue;

        for (var round = 0; round < Rounds && smallest > 0; round++)
        {
            var before = GC.GetAllocatedBytesForCurrentThread();
            action();
            smallest = Math.Min(smallest, GC.GetAllocatedBytesForCurrentThread() - before);
        }

        return smallest;
    }

    /// <summary>Does the same for code that produces a value, which is the one of the last round.</summary>
    /// <typeparam name="TResult">The type of the value.</typeparam>
    /// <param name="action">The code to measure.</param>
    /// <param name="result">The value that the last round produced.</param>
    /// <returns>The smallest number of bytes allocated by one round.</returns>
    internal static long Smallest<TResult>(Func<TResult> action, out TResult result)
    {
        var value = default(TResult)!;
        var smallest = Smallest(() => value = action());

        result = value;

        return smallest;
    }
}
