namespace Wyzely.Functional.Tests;

// Creates tasks that are not completed when they are returned, to test the path of a receiver that completes later.
internal static class Pending
{
    // Returns a task that completes with the value only after it has yielded to the scheduler.
    internal static async Task<T> Of<T>(T value)
    {
        await Task.Yield();

        return value;
    }
}
