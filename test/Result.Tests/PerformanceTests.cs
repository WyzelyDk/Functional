#if NET8_0_OR_GREATER

using System.Runtime.CompilerServices;

namespace Wyzely.Functional.ResultTests;

// These tests back the performance claims of the README with measurements.
public sealed class PerformanceTests
{
    private static readonly Error Rejected = Error.Create("Rejected.");
    private static readonly Task<Result<int>> CompletedResult = Task.FromResult(Result.Success(43));
    private static readonly Task<string> CompletedText = Task.FromResult("done");

    [Fact]
    internal void A_result_without_a_value_is_exactly_one_reference_wide()
    {
        Assert.Equal(nint.Size, Unsafe.SizeOf<Result>());
    }

    [Fact]
    internal void A_result_with_a_reference_value_is_exactly_two_references_wide()
    {
        Assert.Equal(2 * nint.Size, Unsafe.SizeOf<Result<string>>());
    }

    [Fact]
    internal void A_synchronous_bind_with_a_task_delegate_hands_back_the_task_of_the_delegate_without_allocating()
    {
        var handedBack = Result.Success(1).Bind(static _ => CompletedResult);

        HandBackTasks(10);
        var before = GC.GetAllocatedBytesForCurrentThread();
        HandBackTasks(1_000);
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.Multiple(
            () => Assert.Same(CompletedResult, handedBack),
            () => Assert.Equal(0, allocated));
    }

    [Fact]
    internal void A_synchronous_match_with_task_delegates_hands_back_the_task_of_the_delegate()
    {
        var onSuccess = Result.Success(1).Match(static _ => CompletedText, static _ => CompletedText);
        var onFailure = Result.Failure<int>(Rejected).Match(static _ => CompletedText, static _ => CompletedText);

        Assert.Multiple(
            () => Assert.Same(CompletedText, onSuccess),
            () => Assert.Same(CompletedText, onFailure));
    }

    [Fact]
    internal void An_untyped_result_is_a_single_reference()
    {
        Assert.Equal(Unsafe.SizeOf<object>(), Unsafe.SizeOf<Result>());
    }

    [Fact]
    internal void A_typed_result_is_the_value_next_to_a_single_reference()
    {
        Assert.Multiple(
            () => Assert.Equal(2 * Unsafe.SizeOf<object>(), Unsafe.SizeOf<Result<int>>()),
            () => Assert.Equal(2 * Unsafe.SizeOf<object>(), Unsafe.SizeOf<Result<object>>()));
    }

    [Fact]
    internal void A_successful_synchronous_chain_allocates_nothing()
    {
        RunSuccessfulChain(10);

        var before = GC.GetAllocatedBytesForCurrentThread();
        var sum = RunSuccessfulChain(1_000);
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.Multiple(
            () => Assert.Equal(0, allocated),
            () => Assert.True(sum > 0));
    }

    [Fact]
    internal void EnsureNotNull_and_SuccessIfNotNull_allocate_nothing_on_the_success_path()
    {
        RunNullChecks(10);

        var before = GC.GetAllocatedBytesForCurrentThread();
        var sum = RunNullChecks(1_000);
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.Multiple(
            () => Assert.Equal(0, allocated),
            () => Assert.True(sum > 0));
    }

    [Fact]
    internal void Creating_and_inspecting_results_allocates_nothing()
    {
        Inspect(10);

        var before = GC.GetAllocatedBytesForCurrentThread();
        Inspect(1_000);
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.Equal(0, allocated);
    }

    private static long RunNullChecks(int iterations)
    {
        long sum = 0;

        for (var index = 1; index <= iterations; index++)
        {
            var number = Result.Success<int?>(index).EnsureNotNull(Rejected);
            var text = Result.Success<string?>("abc").EnsureNotNull(static () => Rejected);
            var created = Result.SuccessIfNotNull((int?)index, Rejected);

            sum += number.GetValue() + text.GetValue().Length + created.GetValue();
        }

        return sum;
    }

    private static long RunSuccessfulChain(int iterations)
    {
        long sum = 0;

        for (var index = 1; index <= iterations; index++)
        {
            var result = Result.Success(index)
                .Map(static value => value + 1)
                .Ensure(static value => value > 0, Rejected)
                .Ensure(static value => value > 0, static _ => Rejected)
                .Ensure(static value => value > 0, static _ => "Rejected.")
                .MapIf(static value => value > 0, static value => value + 1)
                .BindIf(true, static value => Result.Success(value))
                .TapIf(static () => true, static _ => { })
                .EnsureIf(static value => value > 0, static _ => Result.Success())
                .TapErrorIf(static _ => true, static _ => { })
                .MapTry(static value => value)
                .BindTry(static value => Result.Success(value))
                .TapTry(static _ => { })
                .TapIfTry(true, static _ => { })
                .Tap(static () => { })
                .EnsureNot(static value => value < 0, Rejected)
                .EnsureNot(static value => value < 0, "Negative.")
                .Bind(static value => Result.Success(value * 2))
                .Tap(static _ => { })
                .Ensure(static value => Result.SuccessIf(value > 0, Rejected))
                .MapError(static error => error);

            sum += result.Match(static value => value, static _ => 0);
        }

        return sum;
    }

    private static void HandBackTasks(int iterations)
    {
        for (var index = 0; index < iterations; index++)
        {
            _ = Result.Success(index).Bind(static _ => CompletedResult);
        }
    }

    private static void Inspect(int iterations)
    {
        for (var index = 0; index < iterations; index++)
        {
            var ok = Result.Success(index);
            var failed = Result.Failure<int>(Rejected);

            if (!ok.IsSuccess || !failed.IsFailure || ok.ValueOr(0) != index || failed.ValueOr(-1) != -1)
            {
                throw new InvalidOperationException("Unexpected state.");
            }
        }
    }
}

#endif
