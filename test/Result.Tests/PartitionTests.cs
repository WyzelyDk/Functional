namespace Wyzely.Functional.ResultTests;

public sealed class PartitionTests
{
    [Fact]
    internal void Partition_splits_results_into_values_and_errors_in_order()
    {
        var first = Error.Create("First.");
        var second = Error.Create("Second.");
        IEnumerable<Result<int>> results = [Result.Success(1), Result.Failure<int>(first), Result.Success(3), Result.Failure<int>(second)];

        var (values, errors) = results.Partition();

        Assert.Multiple(
            () => Assert.Equal([1, 3], values),
            () => Assert.Equal([first, second], errors));
    }

    [Fact]
    internal void Partition_of_nothing_returns_two_empty_lists()
    {
        var (values, errors) = Array.Empty<Result<int>>().Partition();

        Assert.Multiple(
            () => Assert.Empty(values),
            () => Assert.Empty(errors));
    }

    [Fact]
    internal void Partition_throws_when_the_sequence_is_null()
    {
        Assert.Throws<ArgumentNullException>(() => ((IEnumerable<Result<int>>)null!).Partition());
    }

    [Fact]
    internal void Partition_treats_a_default_result_as_a_success_with_the_default_value()
    {
        Result<int>[] results = [Result.Success(1), default];

        var (values, errors) = results.Partition();

        Assert.Multiple(
            () => Assert.Equal([1, 0], values),
            () => Assert.Empty(errors));
    }
}
