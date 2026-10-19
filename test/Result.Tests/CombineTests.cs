namespace Wyzely.Functional.ResultTests;

public sealed class CombineTests
{
    private static readonly Error First = Error.Create("First.");
    private static readonly Error Second = Error.Create("Second.");

    [Fact]
    internal void Combine_succeeds_when_every_result_succeeded()
    {
        var result = Result.Combine([Result.Success(), Result.Success()]);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    internal void Combine_of_nothing_succeeds()
    {
        Assert.True(Result.Combine(Array.Empty<Result>()).IsSuccess);
    }

    [Fact]
    internal void Combine_returns_the_single_error_itself_when_one_result_failed()
    {
        var result = Result.Combine([Result.Success(), Result.Failure(First)]);

        Assert.Same(First, result.GetError());
    }

    [Fact]
    internal void Combine_aggregates_the_errors_of_every_failed_result_in_order()
    {
        var result = Result.Combine([Result.Failure(First), Result.Success(), Result.Failure(Second)]);

        Assert.Equal([First, Second], result.GetError().Children);
    }

    [Fact]
    internal void Combine_flattens_aggregate_errors()
    {
        var aggregate = Error.Aggregate([First, Second]);
        var third = Error.Create("Third.");

        var result = Result.Combine([Result.Failure(aggregate), Result.Failure(third)]);

        Assert.Equal([First, Second, third], result.GetError().Children);
    }

    [Fact]
    internal void Combine_treats_a_default_result_as_a_success()
    {
        Result[] results = [Result.Success(), default];

        Assert.True(Result.Combine(results).IsSuccess);
    }

    [Fact]
    internal void Combine_throws_when_the_sequence_is_null()
    {
        Assert.Throws<ArgumentNullException>(() => Result.Combine((IEnumerable<Result>)null!));
    }

    [Fact]
    internal void Combine_is_also_an_extension_method_on_a_sequence_of_results()
    {
        IEnumerable<Result> results = [Result.Success(), Result.Failure(First)];

        Assert.Same(First, results.Combine().GetError());
    }

    [Fact]
    internal void Combine_of_typed_results_returns_the_values_in_order()
    {
        var result = Result.Combine([Result.Success(1), Result.Success(2), Result.Success(3)]);

        Assert.Equal([1, 2, 3], result.GetValue());
    }

    [Fact]
    internal void Combine_of_no_typed_results_returns_an_empty_list()
    {
        var result = Result.Combine(Array.Empty<Result<int>>());

        Assert.Empty(result.GetValue());
    }

    [Fact]
    internal void Combine_of_typed_results_aggregates_the_errors_in_order()
    {
        var result = Result.Combine([Result.Failure<int>(First), Result.Success(2), Result.Failure<int>(Second)]);

        Assert.Equal([First, Second], result.GetError().Children);
    }

    [Fact]
    internal void Combine_of_typed_results_treats_a_default_result_as_a_success_with_the_default_value()
    {
        Result<int>[] results = [Result.Success(1), default];

        Assert.Equal([1, 0], Result.Combine(results).GetValue());
    }

    [Fact]
    internal void Combine_of_typed_results_is_also_an_extension_method()
    {
        IEnumerable<Result<int>> results = [Result.Success(1), Result.Success(2)];

        Assert.Equal([1, 2], results.Combine().GetValue());
    }

    [Fact]
    internal async Task Combine_of_tasks_waits_for_all_of_them_and_combines_the_results()
    {
        var result = await Result.Combine([Task.FromResult(Result.Success()), Task.FromResult(Result.Failure(First)), Task.FromResult(Result.Failure(Second))]);

        Assert.Equal([First, Second], result.GetError().Children);
    }

    [Fact]
    internal async Task Combine_of_typed_tasks_returns_the_values_in_order()
    {
        var result = await Result.Combine([Task.FromResult(Result.Success(1)), Task.FromResult(Result.Success(2))]);

        Assert.Equal([1, 2], result.GetValue());
    }

    [Fact]
    internal async Task Combine_of_value_tasks_combines_the_results()
    {
        var result = await Result.Combine([new ValueTask<Result>(Result.Success()), new ValueTask<Result>(Result.Failure(First))]);

        Assert.Same(First, result.GetError());
    }

    [Fact]
    internal async Task Combine_of_typed_value_tasks_returns_the_values_in_order()
    {
        var result = await Result.Combine([new ValueTask<Result<int>>(Result.Success(1)), new ValueTask<Result<int>>(Result.Success(2))]);

        Assert.Equal([1, 2], result.GetValue());
    }

    [Fact]
    internal async Task Combine_of_tasks_is_also_an_extension_method()
    {
        IEnumerable<Task<Result<int>>> tasks = [Task.FromResult(Result.Success(1)), Task.FromResult(Result.Success(2))];

        var result = await tasks.Combine();

        Assert.Equal([1, 2], result.GetValue());
    }

    [Fact]
    internal async Task Combine_of_tasks_throws_when_the_sequence_is_null()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => Result.Combine((IEnumerable<Task<Result>>)null!));
    }

    [Fact]
    internal async Task Combine_of_tasks_lets_the_exception_of_a_faulted_task_escape()
    {
        var faulted = Task.FromException<Result>(new InvalidOperationException("boom"));

        await Assert.ThrowsAsync<InvalidOperationException>(() => Result.Combine([Task.FromResult(Result.Success()), faulted]));
    }

    [Fact]
    internal async Task Combine_of_tasks_without_a_value_is_also_an_extension_method()
    {
        IEnumerable<Task<Result>> tasks = [Task.FromResult(Result.Success()), Task.FromResult(Result.Failure(First))];

        var result = await tasks.Combine();

        Assert.Same(First, result.GetError());
    }

    [Fact]
    internal async Task Combine_of_value_tasks_without_a_value_is_also_an_extension_method()
    {
        IEnumerable<ValueTask<Result>> tasks = [new ValueTask<Result>(Result.Success()), new ValueTask<Result>(Result.Failure(First))];

        var result = await tasks.Combine();

        Assert.Same(First, result.GetError());
    }

    [Fact]
    internal async Task Combine_of_value_tasks_is_also_an_extension_method()
    {
        IEnumerable<ValueTask<Result<int>>> tasks = [new ValueTask<Result<int>>(Result.Success(1)), new ValueTask<Result<int>>(Result.Success(2))];

        var result = await tasks.Combine();

        Assert.Equal([1, 2], result.GetValue());
    }
}
