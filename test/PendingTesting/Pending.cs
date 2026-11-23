namespace Wyzely.Functional.PendingTesting;

// Creates receivers that are not completed when the method under test gets them, to test the path of a receiver that completes later.
// The receiver completes only when the test says so, and the test fails when the call did not wait for it: a wait of some milliseconds,
// or a yield to the scheduler, can be over before the call looks at the task, which passes the test and silently skips that path.
internal static class Pending
{
    // Gives the call a task that is still pending, checks that the call returned a task that is not completed, completes the receiver with the
    // value and returns what the call produces.
    internal static async Task<TResult> Completing<T, TResult>(T value, Func<Task<T>, Task<TResult>> call)
    {
        var source = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
        var running = call(source.Task);

        Assert.False(running.IsCompleted, "The call returned a completed task: the path for a receiver that completes later was not taken.");
        source.SetResult(value);

        return await running;
    }

    // The same for a method on a value task.
    internal static async ValueTask<TResult> CompletingValue<T, TResult>(T value, Func<ValueTask<T>, ValueTask<TResult>> call)
    {
        var source = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
        var running = call(new ValueTask<T>(source.Task));

        Assert.False(running.IsCompleted, "The call returned a completed value task: the path for a receiver that completes later was not taken.");
        source.SetResult(value);

        return await running;
    }

    // A task that completes after it has yielded to the scheduler. It is only for an operation that has no separate path for a task that has
    // completed (the generated tests of GetValueOrThrow, ThrowIfFailure, SuccessIf, FailureIf and SuccessIfNotNull use it): the same code runs
    // whether the task is completed or not, so nothing depends on the yield being slower than the call.
    internal static async Task<T> Of<T>(T value)
    {
        await Task.Yield();

        return value;
    }
}
