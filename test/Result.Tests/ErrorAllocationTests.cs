#if NET8_0_OR_GREATER

using System.Diagnostics.CodeAnalysis;

namespace Wyzely.Functional.ResultTests;

// An error is immutable, so every With... method copies it. The factories must therefore build their error in one go and not by
// creating one and copying it: a ready-made error costs exactly one object.
public sealed class ErrorAllocationTests
{
    private const int Iterations = 1_000;

    [Fact]
    internal void Unavailable_allocates_as_much_as_a_plain_error()
    {
        var plain = Allocated(static () => Error.Create("Down."));
        var unavailable = Allocated(static () => Error.Unavailable("Down."));

        Assert.Equal(plain, unavailable);
    }

    [Fact]
    internal void Timeout_allocates_as_much_as_a_plain_error()
    {
        var plain = Allocated(static () => Error.Create("Slow."));
        var timeout = Allocated(static () => Error.Timeout("Slow."));

        Assert.Equal(plain, timeout);
    }

    [Fact]
    internal void FromException_allocates_as_much_as_a_plain_error()
    {
        var exception = new InvalidOperationException("Bad.");

        var plain = Allocated(static () => Error.Create("Bad."));
        var fromException = Allocated(() => Error.FromException(exception));

        Assert.Equal(plain, fromException);
    }

    [Fact]
    internal void FromException_for_a_timeout_allocates_as_much_as_a_plain_error()
    {
        var exception = new TimeoutException("Slow.");

        var plain = Allocated(static () => Error.Create("Slow."));
        var fromException = Allocated(() => Error.FromException(exception));

        Assert.Equal(plain, fromException);
    }

    [Fact]
    [SuppressMessage("Sonar", "S3928", Justification = "The parameter name is made up on purpose.")]
    internal void FromException_reads_the_message_of_the_exception_only_once()
    {
        // An ArgumentException builds its message again on every read, so reading it twice shows up as an allocation.
        var exception = new ArgumentException("Bad.", "id");

        var plain = Allocated(() => Error.Create(exception.Message));
        var fromException = Allocated(() => Error.FromException(exception));

        Assert.Equal(plain, fromException);
    }

    [Fact]
    internal void A_frame_without_details_shares_the_empty_list_of_details_and_allocates_less_than_a_frame_with_one()
    {
        var error = Error.Create("Failed.");

        var plain = Allocated(() => error.WithContext("Order"));
        var withDetail = Allocated(() => error.WithContext("Order {Id}", "Id", 1));

        Assert.True(plain < withDetail, $"A frame without details allocated {plain} bytes and one with a detail {withDetail}.");
    }

    // The smallest of several rounds: a one-off allocation from the runtime itself (compiling a method, say) must not decide the test.
    private static long Allocated(Func<Error> create)
    {
        for (var i = 0; i < 200; i++)
        {
            _ = create();
        }

        var smallest = long.MaxValue;

        for (var round = 0; round < 5; round++)
        {
            var before = GC.GetAllocatedBytesForCurrentThread();

            for (var i = 0; i < Iterations; i++)
            {
                _ = create();
            }

            smallest = Math.Min(smallest, GC.GetAllocatedBytesForCurrentThread() - before);
        }

        return smallest;
    }
}

#endif
