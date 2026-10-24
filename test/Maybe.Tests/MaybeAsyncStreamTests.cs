namespace Wyzely.Functional.MaybeTests;

// The questions about a sequence exist for an asynchronous stream too. A stream is read once, in order, and only as far as the answer needs.
public sealed class MaybeAsyncStreamTests
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    [Fact]
    internal async Task FirstOrNone_returns_the_first_element_and_stops_reading()
    {
        var pulled = 0;

        var outcome = await AsyncStreams.Counted(count => pulled = count, 10, 20, 30).FirstOrNone(Token);

        Assert.Multiple(
            () => Assert.Equal(10, outcome.GetValue()),
            () => Assert.Equal(1, pulled));
    }

    [Fact]
    internal async Task FirstOrNone_of_an_empty_stream_is_none()
    {
        var outcome = await AsyncStreams.Of<int>().FirstOrNone(Token);

        Assert.True(outcome.IsNone);
    }

    [Fact]
    internal async Task FirstOrNone_with_a_predicate_returns_the_first_match_and_stops_reading()
    {
        var pulled = 0;

        var outcome = await AsyncStreams.Counted(count => pulled = count, 1, 2, 3, 4).FirstOrNone(static item => item > 1, Token);

        Assert.Multiple(
            () => Assert.Equal(2, outcome.GetValue()),
            () => Assert.Equal(2, pulled));
    }

    [Fact]
    internal async Task FirstOrNone_with_an_asynchronous_predicate_returns_a_value_task()
    {
        var task = AsValueTask(AsyncStreams.Of(1, 2, 3).FirstOrNone(
            static async item =>
            {
                await Task.Yield();

                return item > 1;
            },
            Token));

        var outcome = await task;

        Assert.Equal(2, outcome.GetValue());
    }

    [Fact]
    internal async Task FirstOrNone_with_a_predicate_that_returns_a_value_task_is_awaited()
    {
        var outcome = await AsyncStreams.Of(1, 2, 3).FirstOrNone(static item => new ValueTask<bool>(item > 2), Token);

        Assert.Equal(3, outcome.GetValue());
    }

    [Fact]
    internal async Task FirstOrNone_with_a_predicate_that_nothing_satisfies_is_none()
    {
        var outcome = await AsyncStreams.Of(1, 2).FirstOrNone(static item => item > 5, Token);

        Assert.True(outcome.IsNone);
    }

    [Fact]
    internal async Task LastOrNone_returns_the_last_element()
    {
        var outcome = await AsyncStreams.Of(1, 2, 3).LastOrNone(Token);

        Assert.Equal(3, outcome.GetValue());
    }

    [Fact]
    internal async Task LastOrNone_with_a_predicate_returns_the_last_match_for_a_plain_task_and_value_task_predicate()
    {
        var plain = await AsyncStreams.Of(1, 2, 3, 4).LastOrNone(static item => item < 4, Token);
        var task = await AsyncStreams.Of(1, 2, 3, 4).LastOrNone(static item => Task.FromResult(item < 3), Token);
        var valueTask = await AsyncStreams.Of(1, 2, 3, 4).LastOrNone(static item => new ValueTask<bool>(item < 2), Token);

        Assert.Multiple(
            () => Assert.Equal(3, plain.GetValue()),
            () => Assert.Equal(2, task.GetValue()),
            () => Assert.Equal(1, valueTask.GetValue()));
    }

    [Fact]
    internal async Task LastOrNone_of_an_empty_stream_is_none()
    {
        var outcome = await AsyncStreams.Of<int>().LastOrNone(Token);

        Assert.True(outcome.IsNone);
    }

    [Fact]
    internal async Task SingleOrNone_returns_the_only_element()
    {
        var outcome = await AsyncStreams.Of(7).SingleOrNone(Token);

        Assert.Equal(7, outcome.GetValue());
    }

    [Fact]
    internal async Task SingleOrNone_of_an_empty_stream_is_none()
    {
        var outcome = await AsyncStreams.Of<int>().SingleOrNone(Token);

        Assert.True(outcome.IsNone);
    }

    [Fact]
    internal async Task SingleOrNone_is_none_for_two_elements_and_stops_reading_at_the_second()
    {
        var pulled = 0;

        var outcome = await AsyncStreams.Counted(count => pulled = count, 1, 2, 3).SingleOrNone(Token);

        Assert.Multiple(
            () => Assert.True(outcome.IsNone),
            () => Assert.Equal(2, pulled));
    }

    [Fact]
    internal async Task SingleOrNone_with_a_predicate_returns_the_only_match_for_a_plain_task_and_value_task_predicate()
    {
        var plain = await AsyncStreams.Of(1, 2, 3).SingleOrNone(static item => item == 2, Token);
        var task = await AsyncStreams.Of(1, 2, 3).SingleOrNone(static item => Task.FromResult(item == 3), Token);
        var valueTask = await AsyncStreams.Of(1, 2, 3).SingleOrNone(static item => new ValueTask<bool>(item == 1), Token);
        var several = await AsyncStreams.Of(1, 2, 3).SingleOrNone(static item => item > 1, Token);

        Assert.Multiple(
            () => Assert.Equal(2, plain.GetValue()),
            () => Assert.Equal(3, task.GetValue()),
            () => Assert.Equal(1, valueTask.GetValue()),
            () => Assert.True(several.IsNone));
    }

    [Fact]
    internal async Task ElementAtOrNone_returns_the_element_at_the_index_and_stops_reading()
    {
        var pulled = 0;

        var outcome = await AsyncStreams.Counted(count => pulled = count, 10, 20, 30, 40).ElementAtOrNone(1, Token);

        Assert.Multiple(
            () => Assert.Equal(20, outcome.GetValue()),
            () => Assert.Equal(2, pulled));
    }

    [Fact]
    internal async Task ElementAtOrNone_is_none_for_an_index_beyond_the_stream_or_below_zero()
    {
        var beyond = await AsyncStreams.Of(1, 2).ElementAtOrNone(2, Token);
        var negative = await AsyncStreams.Of(1, 2).ElementAtOrNone(-1, Token);

        Assert.Multiple(
            () => Assert.True(beyond.IsNone),
            () => Assert.True(negative.IsNone));
    }

    [Fact]
    internal async Task Choose_keeps_the_values_of_the_somes_in_order()
    {
        var values = await AsyncStreams.ToListAsync(
            AsyncStreams.Of(Maybe.Some(1), Maybe.None<int>(), Maybe.Some(3)).Choose(),
            Token);

        Assert.Equal([1, 3], values);
    }

    [Fact]
    internal async Task Choose_with_a_selector_keeps_the_values_of_the_somes_for_a_plain_task_and_value_task_selector()
    {
        var plain = await AsyncStreams.ToListAsync(
            AsyncStreams.Of(1, 2, 3).Choose(static item => item == 2 ? Maybe.None<string>() : Maybe.Some(Describe(item))),
            Token);
        var task = await AsyncStreams.ToListAsync(
            AsyncStreams.Of(1, 2, 3).Choose(static async item =>
            {
                await Task.Yield();

                return item > 1 ? Maybe.Some(item * 10) : Maybe.None<int>();
            }),
            Token);
        var valueTask = await AsyncStreams.ToListAsync(
            AsyncStreams.Of(1, 2, 3).Choose(static item => new ValueTask<Maybe<int>>(item < 3 ? Maybe.Some(item) : Maybe.None<int>())),
            Token);

        Assert.Multiple(
            () => Assert.Equal(["1", "3"], plain),
            () => Assert.Equal([20, 30], task),
            () => Assert.Equal([1, 2], valueTask));
    }

    [Fact]
    internal async Task Choose_reads_the_stream_only_as_far_as_the_consumer_goes()
    {
        var pulled = 0;
        var stream = AsyncStreams.Counted(count => pulled = count, Maybe.Some(1), Maybe.Some(2), Maybe.Some(3)).Choose();

        await using var enumerator = stream.GetAsyncEnumerator(Token);
        await enumerator.MoveNextAsync();

        Assert.Equal(1, pulled);
    }

    [Fact]
    internal async Task The_terminal_operations_stop_when_the_token_is_cancelled()
    {
        using var source = CancellationTokenSource.CreateLinkedTokenSource(Token);
        await source.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await AsyncStreams.Cancellable([1, 2]).FirstOrNone(source.Token));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await AsyncStreams.Cancellable([1, 2]).LastOrNone(source.Token));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await AsyncStreams.Cancellable([1, 2]).SingleOrNone(source.Token));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await AsyncStreams.Cancellable([1, 2]).ElementAtOrNone(1, source.Token));
    }

    [Fact]
    internal async Task Choose_stops_when_the_token_of_the_consumer_is_cancelled()
    {
        using var source = CancellationTokenSource.CreateLinkedTokenSource(Token);
        await source.CancelAsync();
        var stream = AsyncStreams.Cancellable([Maybe.Some(1)]).Choose();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await AsyncStreams.ToListAsync(stream, source.Token));
    }

    [Fact]
    internal void The_operations_throw_when_the_stream_or_a_delegate_is_null()
    {
        var stream = AsyncStreams.Of(1);

        Assert.Multiple(
            () => Assert.Equal("source", Assert.Throws<ArgumentNullException>(() => ((IAsyncEnumerable<int>)null!).FirstOrNone(Token)).ParamName),
            () => Assert.Equal("source", Assert.Throws<ArgumentNullException>(() => ((IAsyncEnumerable<int>)null!).LastOrNone(Token)).ParamName),
            () => Assert.Equal("source", Assert.Throws<ArgumentNullException>(() => ((IAsyncEnumerable<int>)null!).SingleOrNone(Token)).ParamName),
            () => Assert.Equal("source", Assert.Throws<ArgumentNullException>(() => ((IAsyncEnumerable<int>)null!).ElementAtOrNone(0, Token)).ParamName),
            () => Assert.Equal("source", Assert.Throws<ArgumentNullException>(() => ((IAsyncEnumerable<Maybe<int>>)null!).Choose()).ParamName),
            () => Assert.Equal("predicate", Assert.Throws<ArgumentNullException>(() => stream.FirstOrNone((Func<int, bool>)null!, Token)).ParamName),
            () => Assert.Equal("predicate", Assert.Throws<ArgumentNullException>(() => stream.SingleOrNone(null!, Token)).ParamName),
            () => Assert.Equal("selector", Assert.Throws<ArgumentNullException>(() => stream.Choose((Func<int, Maybe<int>>)null!)).ParamName));
    }

    private static string Describe(int item) => item.ToString(System.Globalization.CultureInfo.InvariantCulture);

    // Passing the call through a parameter of an exact type pins the type the overload returns.
    private static ValueTask<Maybe<int>> AsValueTask(ValueTask<Maybe<int>> task) => task;
}
