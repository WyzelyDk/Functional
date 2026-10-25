namespace Wyzely.Functional.Tests;

// Creates tasks that are not completed when they are returned, to test the path of a receiver that completes later.
internal static class Pending
{
    // Returns a task that completes with the value only after a short wait. Yielding to the scheduler is not enough: the code that is tested checks whether
    // the task has completed right after it gets it, and with the instrumentation of a coverage run the other thread can win that race, so that the path
    // for a receiver that completes later is not taken. A wait of a few milliseconds is far longer than that check.
    internal static async Task<T> Of<T>(T value)
    {
        await Task.Delay(TimeSpan.FromMilliseconds(20));

        return value;
    }
}
