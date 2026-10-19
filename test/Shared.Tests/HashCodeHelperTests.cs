namespace Wyzely.Functional.SharedTests;

public sealed class HashCodeHelperTests
{
    [Fact]
    internal void Combine_is_deterministic_for_the_same_values()
    {
        Assert.Multiple(
            () => Assert.Equal(HashCodeHelper.Combine(1, 2), HashCodeHelper.Combine(1, 2)),
            () => Assert.Equal(HashCodeHelper.Combine(1, 2, 3), HashCodeHelper.Combine(1, 2, 3)),
            () => Assert.Equal(HashCodeHelper.Combine(1, 2, 3, 4), HashCodeHelper.Combine(1, 2, 3, 4)));
    }

    [Fact]
    internal void Combine_depends_on_the_order_of_the_values()
    {
        Assert.Multiple(
            () => Assert.NotEqual(HashCodeHelper.Combine(1, 2), HashCodeHelper.Combine(2, 1)),
            () => Assert.NotEqual(HashCodeHelper.Combine(1, 2, 3), HashCodeHelper.Combine(3, 2, 1)),
            () => Assert.NotEqual(HashCodeHelper.Combine(1, 2, 3, 4), HashCodeHelper.Combine(4, 3, 2, 1)));
    }

    [Fact]
    internal void Combine_depends_on_every_value()
    {
        Assert.Multiple(
            () => Assert.NotEqual(HashCodeHelper.Combine(1, 2, 3), HashCodeHelper.Combine(1, 2, 4)),
            () => Assert.NotEqual(HashCodeHelper.Combine(1, 2, 3, 4), HashCodeHelper.Combine(1, 2, 3, 5)));
    }
}
