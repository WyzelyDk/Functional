using Xunit.Sdk;

namespace Wyzely.Functional.Tests;

// The tests of the interop methods rely on Pending to take the path of a receiver that completes later. A call that was handed a completed task
// would pass them all and only lose the coverage of that path, so Pending has to fail when it was not pending.
public sealed class PendingTests
{
    [Fact]
    internal async Task Completing_hands_the_call_a_task_that_has_not_completed_and_returns_the_result_after_completing_it()
    {
        var completedWhenGiven = true;

        var result = await Pending.Completing(5, async task =>
        {
            completedWhenGiven = task.IsCompleted;

            return await task + 1;
        });

        Assert.Multiple(
            () => Assert.False(completedWhenGiven),
            () => Assert.Equal(6, result));
    }

    [Fact]
    internal async Task Completing_fails_when_the_call_returns_a_task_that_has_already_completed()
    {
        await Assert.ThrowsAsync<FalseException>(() => Pending.Completing(5, static _ => Task.FromResult(6)));
    }

    [Fact]
    internal async Task CompletingValue_hands_the_call_a_value_task_that_has_not_completed_and_returns_the_result_after_completing_it()
    {
        var completedWhenGiven = true;

        var result = await Pending.CompletingValue(5, async task =>
        {
            completedWhenGiven = task.IsCompleted;

            return await task + 1;
        });

        Assert.Multiple(
            () => Assert.False(completedWhenGiven),
            () => Assert.Equal(6, result));
    }

    [Fact]
    internal async Task CompletingValue_fails_when_the_call_returns_a_value_task_that_has_already_completed()
    {
        await Assert.ThrowsAsync<FalseException>(() => Pending.CompletingValue(5, static _ => new ValueTask<int>(6)).AsTask());
    }
}
