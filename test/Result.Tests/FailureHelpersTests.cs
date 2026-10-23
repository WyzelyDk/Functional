namespace Wyzely.Functional.ResultTests;

// ConvertFailure passes a failure on as a failure of another type, FirstFailureOrSuccess stops at the first failure, and CombineInOrder runs the tasks
// of a sequence one at a time. Combine, by contrast, starts every task first and looks at every result.
public sealed class FailureHelpersTests
{
    private static readonly Error First = Error.Create("First.");
    private static readonly Error Second = Error.Create("Second.");

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    [Fact]
    internal void ConvertFailure_returns_the_same_error_as_a_result_of_another_type()
    {
        var outcome = Result.Failure<int>(First).ConvertFailure<string>();

        Assert.Same(First, outcome.GetError());
    }

    [Fact]
    internal void ConvertFailure_of_an_untyped_result_returns_the_same_error_as_a_typed_one()
    {
        var outcome = Result.Failure(First).ConvertFailure<string>();

        Assert.Same(First, outcome.GetError());
    }

    [Fact]
    internal void ConvertFailure_throws_when_the_result_succeeded_and_says_how_to_check()
    {
        var typed = Record.Exception(() => Result.Success(2).ConvertFailure<string>());
        var untyped = Record.Exception(() => Result.Success().ConvertFailure<string>());

        Assert.Multiple(
            () => Assert.IsType<InvalidOperationException>(typed),
            () => Assert.Contains("IsFailure", Assert.IsType<InvalidOperationException>(untyped).Message, StringComparison.Ordinal));
    }

    [Fact]
    internal void FirstFailureOrSuccess_returns_the_first_failure()
    {
        var outcome = new[] { Result.Success(), Result.Failure(First), Result.Failure(Second) }.FirstFailureOrSuccess();

        Assert.Same(First, outcome.GetError());
    }

    [Fact]
    internal void FirstFailureOrSuccess_succeeds_when_nothing_failed_or_the_sequence_is_empty()
    {
        Assert.Multiple(
            () => Assert.True(new[] { Result.Success(), Result.Success() }.FirstFailureOrSuccess().IsSuccess),
            () => Assert.True(Array.Empty<Result>().FirstFailureOrSuccess().IsSuccess));
    }

    [Fact]
    internal void FirstFailureOrSuccess_does_not_look_further_than_the_first_failure()
    {
        var pulled = 0;

        IEnumerable<Result> Lazy()
        {
            pulled++;
            yield return Result.Success();
            pulled++;
            yield return Result.Failure(First);
            pulled++;
            yield return Result.Failure(Second);
        }

        var outcome = Lazy().FirstFailureOrSuccess();

        Assert.Multiple(
            () => Assert.Same(First, outcome.GetError()),
            () => Assert.Equal(2, pulled));
    }

    [Fact]
    internal void FirstFailureOrSuccess_throws_when_the_sequence_is_null()
    {
        var exception = Record.Exception(() => ((IEnumerable<Result>)null!).FirstFailureOrSuccess());

        Assert.Equal("results", Assert.IsType<ArgumentNullException>(exception).ParamName);
    }

    [Fact]
    internal async Task FirstFailureOrSuccess_of_a_stream_returns_the_first_failure_and_stops_reading()
    {
        var pulled = 0;

        var outcome = await AsyncStreams.Counted(count => pulled = count, Result.Success(), Result.Failure(First), Result.Failure(Second))
            .FirstFailureOrSuccess(Token);

        Assert.Multiple(
            () => Assert.Same(First, outcome.GetError()),
            () => Assert.Equal(2, pulled));
    }

    [Fact]
    internal async Task FirstFailureOrSuccess_of_a_stream_succeeds_when_nothing_failed()
    {
        var outcome = await AsyncStreams.Of(Result.Success(), Result.Success()).FirstFailureOrSuccess(Token);

        Assert.True(outcome.IsSuccess);
    }

    [Fact]
    internal async Task CombineInOrder_runs_the_tasks_of_a_lazy_sequence_one_after_another()
    {
        var log = new List<string>();

        IEnumerable<Task<Result>> Lazy()
        {
            foreach (var name in new[] { "a", "b", "c" })
            {
                log.Add("create " + name);

                yield return RunAsync(name);
            }
        }

        async Task<Result> RunAsync(string name)
        {
            log.Add("start " + name);
            await Task.Yield();
            log.Add("end " + name);

            return Result.Success();
        }

        var outcome = await Lazy().CombineInOrder();

        Assert.Multiple(
            () => Assert.True(outcome.IsSuccess),
            () => Assert.Equal(
                ["create a", "start a", "end a", "create b", "start b", "end b", "create c", "start c", "end c"],
                log));
    }

    [Fact]
    internal async Task Combine_by_contrast_starts_every_task_before_it_waits_for_one()
    {
        var log = new List<string>();

        IEnumerable<Task<Result>> Lazy()
        {
            foreach (var name in new[] { "a", "b" })
            {
                yield return RunAsync(name);
            }
        }

        async Task<Result> RunAsync(string name)
        {
            log.Add("start " + name);
            await Task.Yield();

            return Result.Success();
        }

        await Lazy().Combine();

        Assert.Equal(["start a", "start b"], log);
    }

    [Fact]
    internal async Task CombineInOrder_aggregates_the_errors_of_the_failed_results_in_order()
    {
        var outcome = await new[]
        {
            Task.FromResult(Result.Failure(First)),
            Task.FromResult(Result.Success()),
            Task.FromResult(Result.Failure(Second)),
        }.CombineInOrder();

        Assert.Equal([First, Second], outcome.GetError().Children);
    }

    [Fact]
    internal async Task CombineInOrder_collects_the_values_in_order()
    {
        var outcome = await new[] { Task.FromResult(Result.Success(1)), Task.FromResult(Result.Success(2)) }.CombineInOrder();

        Assert.Equal([1, 2], outcome.GetValue());
    }

    [Fact]
    internal async Task CombineInOrder_of_value_tasks_runs_one_after_another_and_combines()
    {
        var plain = await new[] { new ValueTask<Result>(Result.Success()), new ValueTask<Result>(Result.Failure(First)) }.CombineInOrder();
        var typed = await new[] { new ValueTask<Result<int>>(Result.Success(1)), new ValueTask<Result<int>>(Result.Success(2)) }.CombineInOrder();

        Assert.Multiple(
            () => Assert.Same(First, plain.GetError()),
            () => Assert.Equal([1, 2], typed.GetValue()));
    }

    [Fact]
    internal async Task CombineInOrder_lets_an_exception_escape_and_does_not_run_the_tasks_after_it()
    {
        var calls = 0;

        IEnumerable<Task<Result>> Lazy()
        {
            yield return Task.FromException<Result>(new InvalidOperationException("Boom."));
            calls++;
            yield return Task.FromResult(Result.Success());
        }

        await Assert.ThrowsAsync<InvalidOperationException>(() => Lazy().CombineInOrder());

        Assert.Equal(0, calls);
    }

    [Fact]
    internal void CombineInOrder_throws_when_the_sequence_is_null()
    {
        var exception = Record.Exception(() => (object)((IEnumerable<Task<Result>>)null!).CombineInOrder());

        Assert.Equal("results", Assert.IsType<ArgumentNullException>(exception).ParamName);
    }
}
