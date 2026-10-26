namespace Wyzely.Functional.ResultTests;

public sealed class AllocationProbeTests
{
    [Fact]
    internal void Smallest_reports_what_code_allocates_in_every_round()
    {
        object? kept = null;

        var allocated = AllocationProbe.Smallest(() => kept = new byte[1_000]);

        Assert.Multiple(
            () => Assert.True(allocated >= 1_000),
            () => Assert.NotNull(kept));
    }

    [Fact]
    internal void Smallest_reports_nothing_for_code_that_allocates_only_in_the_first_round()
    {
        var round = 0;
        object? kept = null;

        var allocated = AllocationProbe.Smallest(() =>
        {
            if (round++ == 0)
            {
                kept = new byte[1_000];
            }
        });

        Assert.Multiple(
            () => Assert.Equal(0, allocated),
            () => Assert.NotNull(kept));
    }

    [Fact]
    internal void Smallest_returns_the_value_of_the_code_that_it_measured()
    {
        var allocated = AllocationProbe.Smallest(static () => 42, out var value);

        Assert.Multiple(
            () => Assert.Equal(0, allocated),
            () => Assert.Equal(42, value));
    }
}
