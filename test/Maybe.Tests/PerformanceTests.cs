#if NET8_0_OR_GREATER

using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Wyzely.Functional.MaybeTests;

// These tests back the performance claims of the README with measurements.
public sealed class PerformanceTests
{
    // An unoptimised JIT does not fold the null check on a generic value, so Debug boxes every Some of an int: the claim is about optimised code.
    private const string UnoptimisedReason = "The library is built without optimisation (Debug); the allocation claim is about optimised code.";

    // A result that is only stored in a field cannot be optimised away: a real chain hands its task to somebody.
    private static readonly Task<Maybe<string>> CompletedNone = Task.FromResult(Maybe.None<string>());
    private static Task? sink;

    [Fact]
    internal void A_maybe_of_an_int_is_the_value_and_a_flag_in_one_word()
    {
        Assert.Equal(8, Unsafe.SizeOf<Maybe<int>>());
    }

    [Fact]
    internal void A_maybe_of_a_reference_is_the_reference_and_a_flag()
    {
        Assert.Equal(2 * nint.Size, Unsafe.SizeOf<Maybe<string>>());
    }

    [Fact]
    internal void A_successful_synchronous_chain_allocates_nothing()
    {
        Assert.SkipWhen(LibraryIsUnoptimised(), UnoptimisedReason);

        RunChain(10);
        var allocated = AllocationProbe.Smallest(() => RunChain(1_000), out var total);

        Assert.Multiple(
            () => Assert.Equal(1_000 * 22, total),
            () => Assert.Equal(0, allocated));
    }

    [Fact]
    internal void A_chain_that_ends_in_none_allocates_nothing()
    {
        Assert.SkipWhen(LibraryIsUnoptimised(), UnoptimisedReason);

        RunNoneChain(10);
        var allocated = AllocationProbe.Smallest(() => RunNoneChain(1_000));

        Assert.Equal(0, allocated);
    }

    [Fact]
    internal void A_task_chain_of_a_reference_type_that_stays_none_allocates_nothing()
    {
        RunNoneTaskChain(10);
        var allocated = AllocationProbe.Smallest(() => RunNoneTaskChain(1_000));

        Assert.Multiple(
            () => Assert.NotNull(Volatile.Read(ref sink)),
            () => Assert.Equal(0, allocated));
    }

    [Fact]
    internal void A_synchronous_bind_with_a_task_delegate_on_none_of_a_reference_type_allocates_nothing()
    {
        var none = Maybe.None<string>();

        BindNoneToTask(none, 10);
        var allocated = AllocationProbe.Smallest(() => BindNoneToTask(none, 1_000));

        Assert.Multiple(
            () => Assert.NotNull(Volatile.Read(ref sink)),
            () => Assert.Equal(0, allocated));
    }

    private static bool LibraryIsUnoptimised() =>
        typeof(Maybe).Assembly.GetCustomAttribute<DebuggableAttribute>() is { IsJITOptimizerDisabled: true };

    private static void RunNoneTaskChain(int iterations)
    {
        for (var i = 0; i < iterations; i++)
        {
            var mapped = CompletedNone.Map(static x => x + "!");
            var filtered = mapped.Filter(static x => x.Length > 5);
            Volatile.Write(ref sink, filtered.Bind(static x => Maybe.Some(x + x)));
        }
    }

    private static void BindNoneToTask(Maybe<string> none, int iterations)
    {
        for (var i = 0; i < iterations; i++)
        {
            Volatile.Write(ref sink, none.Bind(static x => Task.FromResult(Maybe.Some(x))));
        }
    }

    private static int RunChain(int iterations)
    {
        var total = 0;

        for (var i = 0; i < iterations; i++)
        {
            total += Maybe.Some(10)
                .Map(static x => x + 1)
                .Filter(static x => x > 5)
                .Bind(static x => Maybe.Some(x * 2))
                .Match(static x => x, static () => 0);
        }

        return total;
    }

    private static void RunNoneChain(int iterations)
    {
        for (var i = 0; i < iterations; i++)
        {
            _ = Maybe.Some(1)
                .Filter(static x => x > 5)
                .Map(static x => x + 1)
                .ValueOr(0);
        }
    }
}

#endif
