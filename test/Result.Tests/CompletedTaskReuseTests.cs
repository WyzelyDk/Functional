namespace Wyzely.Functional.ResultTests;

// A step on an already completed task that leaves the result as it was hands back the task it received instead of allocating a new one.
// The steps that can change the value (Map, Bind) must never do that, so they are pinned here as well.
public sealed class CompletedTaskReuseTests
{
    private static readonly Error Rejected = Error.Create("Rejected.");
    private static readonly Error Other = Error.Create("Other.");

    [Fact]
    internal void Ensure_returns_the_receiver_task_when_the_condition_holds()
    {
        var receiver = Task.FromResult(Result.Success(2));

        var returned = receiver.Ensure(static x => x > 1, Rejected);

        Assert.Same(receiver, returned);
    }

    [Fact]
    internal void Ensure_returns_the_receiver_task_when_the_receiver_failed()
    {
        var receiver = Task.FromResult(Result.Failure<int>(Other));

        var returned = receiver.Ensure(static x => x > 1, Rejected);

        Assert.Same(receiver, returned);
    }

    [Fact]
    internal void Ensure_returns_a_new_task_with_the_given_error_when_the_condition_does_not_hold()
    {
        var receiver = Task.FromResult(Result.Success(2));

        var returned = receiver.Ensure(static x => x > 5, Rejected);

        Assert.Multiple(
            () => Assert.NotSame(receiver, returned),
            () => Assert.Same(Rejected, returned.Result.GetError()));
    }

    [Fact]
    internal void Ensure_with_a_message_returns_the_receiver_task_when_the_condition_holds()
    {
        var receiver = Task.FromResult(Result.Success(2));

        var returned = receiver.Ensure(static x => x > 1, "Rejected.");

        Assert.Same(receiver, returned);
    }

    [Fact]
    internal void Ensure_with_a_message_returns_a_new_task_when_the_condition_does_not_hold()
    {
        var receiver = Task.FromResult(Result.Success(2));

        var returned = receiver.Ensure(static x => x > 5, "Rejected.");

        Assert.Multiple(
            () => Assert.NotSame(receiver, returned),
            () => Assert.True(returned.Result.IsFailure));
    }

    [Fact]
    internal void Ensure_with_an_error_factory_returns_the_receiver_task_when_the_condition_holds()
    {
        var receiver = Task.FromResult(Result.Success(2));

        var returned = receiver.Ensure(static x => x > 1, static _ => Rejected);

        Assert.Same(receiver, returned);
    }

    [Fact]
    internal void Ensure_with_an_error_factory_returns_the_receiver_task_when_the_receiver_failed()
    {
        var receiver = Task.FromResult(Result.Failure<int>(Other));

        var returned = receiver.Ensure(static x => x > 1, static _ => Rejected);

        Assert.Same(receiver, returned);
    }

    [Fact]
    internal void Ensure_with_an_error_factory_returns_a_new_task_with_the_created_error_when_the_condition_does_not_hold()
    {
        var receiver = Task.FromResult(Result.Success(2));

        var returned = receiver.Ensure(static x => x > 5, static _ => Rejected);

        Assert.Multiple(
            () => Assert.NotSame(receiver, returned),
            () => Assert.Same(Rejected, returned.Result.GetError()));
    }

    [Fact]
    internal void Ensure_with_a_message_factory_returns_the_receiver_task_when_the_condition_holds()
    {
        var receiver = Task.FromResult(Result.Success(2));

        var returned = receiver.Ensure(static x => x > 1, static _ => "Rejected.");

        Assert.Same(receiver, returned);
    }

    [Fact]
    internal void Ensure_with_a_validation_returns_the_receiver_task_when_the_validation_succeeds()
    {
        var receiver = Task.FromResult(Result.Success(2));

        var returned = receiver.Ensure(static _ => Result.Success());

        Assert.Same(receiver, returned);
    }

    [Fact]
    internal void Ensure_with_a_validation_returns_a_new_task_with_the_error_of_the_validation_when_it_fails()
    {
        var receiver = Task.FromResult(Result.Success(2));

        var returned = receiver.Ensure(static _ => Result.Failure(Rejected));

        Assert.Multiple(
            () => Assert.NotSame(receiver, returned),
            () => Assert.Same(Rejected, returned.Result.GetError()));
    }

    [Fact]
    internal void Tap_returns_the_receiver_task_for_a_success_and_for_a_failure()
    {
        var success = Task.FromResult(Result.Success(2));
        var failure = Task.FromResult(Result.Failure<int>(Rejected));

        Assert.Multiple(
            () => Assert.Same(success, success.Tap(static _ => { })),
            () => Assert.Same(failure, failure.Tap(static _ => { })));
    }

    [Fact]
    internal void TapError_returns_the_receiver_task_for_a_success_and_for_a_failure()
    {
        var success = Task.FromResult(Result.Success(2));
        var failure = Task.FromResult(Result.Failure<int>(Rejected));

        Assert.Multiple(
            () => Assert.Same(success, success.TapError(static _ => { })),
            () => Assert.Same(failure, failure.TapError(static _ => { })));
    }

    [Fact]
    internal void MapError_returns_the_receiver_task_for_a_success()
    {
        var receiver = Task.FromResult(Result.Success(2));

        var returned = receiver.MapError(static _ => Other);

        Assert.Same(receiver, returned);
    }

    [Fact]
    internal void MapError_returns_the_receiver_task_when_the_error_is_mapped_to_itself()
    {
        var receiver = Task.FromResult(Result.Failure<int>(Rejected));

        var returned = receiver.MapError(static error => error);

        Assert.Same(receiver, returned);
    }

    [Fact]
    internal void MapError_returns_a_new_task_with_the_mapped_error_when_the_error_changes()
    {
        var receiver = Task.FromResult(Result.Failure<int>(Rejected));

        var returned = receiver.MapError(static _ => Other);

        Assert.Multiple(
            () => Assert.NotSame(receiver, returned),
            () => Assert.Same(Other, returned.Result.GetError()));
    }

    [Fact]
    internal void Recover_returns_the_receiver_task_for_a_success()
    {
        var receiver = Task.FromResult(Result.Success(2));

        var returned = receiver.Recover(static _ => 7);

        Assert.Same(receiver, returned);
    }

    [Fact]
    internal void Recover_returns_a_new_task_with_the_replacement_value_for_a_failure()
    {
        var receiver = Task.FromResult(Result.Failure<int>(Rejected));

        var returned = receiver.Recover(static _ => 7);

        Assert.Multiple(
            () => Assert.NotSame(receiver, returned),
            () => Assert.Equal(7, returned.Result.GetValue()));
    }

    [Fact]
    internal void Compensate_returns_the_receiver_task_for_a_success()
    {
        var receiver = Task.FromResult(Result.Success(2));

        var returned = receiver.Compensate(static _ => Result.Success(7));

        Assert.Same(receiver, returned);
    }

    [Fact]
    internal void Compensate_returns_a_new_task_with_the_fallback_for_a_failure()
    {
        var receiver = Task.FromResult(Result.Failure<int>(Rejected));

        var returned = receiver.Compensate(static _ => Result.Success(7));

        Assert.Multiple(
            () => Assert.NotSame(receiver, returned),
            () => Assert.Equal(7, returned.Result.GetValue()));
    }

    [Fact]
    internal void Untyped_steps_return_the_receiver_task_when_the_result_is_left_as_it_was()
    {
        var success = Task.FromResult(Result.Success());
        var failure = Task.FromResult(Result.Failure(Rejected));

        Assert.Multiple(
            () => Assert.Same(success, success.Tap(static () => { })),
            () => Assert.Same(failure, failure.Tap(static () => { })),
            () => Assert.Same(success, success.TapError(static _ => { })),
            () => Assert.Same(failure, failure.TapError(static _ => { })),
            () => Assert.Same(success, success.MapError(static _ => Other)),
            () => Assert.Same(failure, failure.MapError(static error => error)),
            () => Assert.Same(success, success.Compensate(static _ => Result.Success())));
    }

    [Fact]
    internal void Untyped_steps_return_a_new_task_when_the_result_changes()
    {
        var failure = Task.FromResult(Result.Failure(Rejected));

        var mapped = failure.MapError(static _ => Other);
        var compensated = failure.Compensate(static _ => Result.Success());

        Assert.Multiple(
            () => Assert.NotSame(failure, mapped),
            () => Assert.Same(Other, mapped.Result.GetError()),
            () => Assert.NotSame(failure, compensated),
            () => Assert.True(compensated.Result.IsSuccess));
    }

    [Fact]
    internal void Map_returns_a_task_with_the_new_value_even_though_the_result_is_a_success_either_way()
    {
        var receiver = Task.FromResult(Result.Success(2));

        var returned = receiver.Map(static x => x + 1);

        Assert.Multiple(
            () => Assert.NotSame(receiver, returned),
            () => Assert.Equal(3, returned.Result.GetValue()));
    }

    [Fact]
    internal void Bind_returns_a_task_with_the_new_value_even_though_the_result_is_a_success_either_way()
    {
        var receiver = Task.FromResult(Result.Success(2));

        var returned = receiver.Bind(static x => Result.Success(x * 10));

        Assert.Multiple(
            () => Assert.NotSame(receiver, returned),
            () => Assert.Equal(20, returned.Result.GetValue()));
    }

#if NET8_0_OR_GREATER
    [Fact]
    internal void A_chain_of_steps_that_leave_the_result_as_it_was_allocates_nothing_on_a_completed_task()
    {
        var receiver = Task.FromResult(Result.Success(2));

        RunChain(receiver, 10);
        var before = GC.GetAllocatedBytesForCurrentThread();
        RunChain(receiver, 1_000);
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.Equal(0, allocated);
    }

    private static void RunChain(Task<Result<int>> receiver, int iterations)
    {
        for (var i = 0; i < iterations; i++)
        {
            _ = receiver
                .Ensure(static x => x > 0, Rejected)
                .Ensure(static _ => Result.Success())
                .Tap(static _ => { })
                .TapError(static _ => { })
                .MapError(static error => error)
                .Recover(static _ => 0)
                .Compensate(static _ => Result.Success(0));
        }
    }
#endif
}
