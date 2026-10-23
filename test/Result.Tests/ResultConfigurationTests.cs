using System.Diagnostics.CodeAnalysis;

namespace Wyzely.Functional.ResultTests;

[Collection(ProcessWideConfiguration.Name)]
public sealed class ResultConfigurationTests : IDisposable
{
    public void Dispose() => ResultConfiguration.Reset();

    [Fact]
    internal void The_defaults_are_the_documented_ones()
    {
        var aggregate = Error.Aggregate([Error.Create("A."), Error.Create("B.")]);

        Assert.Multiple(
            () => Assert.Equal("One or more errors occurred.", aggregate.Template),
            () => Assert.Equal("A., B.", aggregate.Message),
            () => Assert.Equal("Meny: Boom.", Error.Create("Boom.").WithContext("Meny").ToString()),
            () => Assert.Equal("The operation was cancelled.", Error.Cancelled().Message),
            () => Assert.Equal("password=***", new ErrorDetail("password", "secret", isSensitive: true).ToString()));
    }

    [Fact]
    internal void AggregateMessage_changes_the_template_of_new_aggregate_errors()
    {
        ResultConfiguration.Configure(options => options.AggregateMessage = "Several things went wrong.");

        var aggregate = Error.Aggregate([Error.Create("A."), Error.Create("B.")]);

        Assert.Equal("Several things went wrong.", aggregate.Template);
    }

    [Fact]
    internal void ChildSeparator_changes_how_an_aggregate_is_shown_as_one_message()
    {
        ResultConfiguration.Configure(options => options.ChildSeparator = " | ");

        var aggregate = Error.Aggregate([Error.Create("A."), Error.Create("B.")]);

        Assert.Equal("A. | B.", aggregate.Message);
    }

    [Fact]
    internal void ContextSeparator_changes_how_context_frames_are_joined_to_the_message()
    {
        ResultConfiguration.Configure(options => options.ContextSeparator = " > ");

        var error = Error.Create("Boom.").WithContext("Meny");

        Assert.Equal("Meny > Boom.", error.ToString());
    }

    [Fact]
    internal void CancelledMessage_changes_the_default_message_of_a_cancelled_error()
    {
        ResultConfiguration.Configure(options => options.CancelledMessage = "Stopped.");

        Assert.Multiple(
            () => Assert.Equal("Stopped.", Error.Cancelled().Message),
            () => Assert.Equal("Custom.", Error.Cancelled("Custom.").Message));
    }

    [Fact]
    internal void RedactedValue_changes_what_replaces_a_sensitive_detail()
    {
        ResultConfiguration.Configure(options => options.RedactedValue = "<hidden>");

        Assert.Equal("password=<hidden>", new ErrorDetail("password", "secret", isSensitive: true).ToString());
    }

    [Fact]
    internal void ExceptionMapper_decides_the_error_of_Try_and_FromException_when_no_handler_is_given()
    {
        ResultConfiguration.Configure(options => options.ExceptionMapper = exception => Error.Create("Mapped: " + exception.Message).WithCode("MAPPED"));

        var fromTry = Result.Try((Action)(() => throw new InvalidOperationException("boom")));
        var fromException = Result.FromException<int>(new InvalidOperationException("boom"));

        Assert.Multiple(
            () => Assert.Equal("MAPPED", fromTry.GetError().Code),
            () => Assert.Equal("Mapped: boom", fromTry.GetError().Message),
            () => Assert.Equal("MAPPED", fromException.GetError().Code));
    }

    [Fact]
    internal void An_explicit_handler_wins_over_the_ExceptionMapper()
    {
        ResultConfiguration.Configure(options => options.ExceptionMapper = _ => Error.Create("Mapped."));

        var result = Result.Try((Action)(() => throw new InvalidOperationException("boom")), _ => Error.Create("Explicit."));

        Assert.Equal("Explicit.", result.GetError().Message);
    }

    [Fact]
    internal void ShouldCatch_can_make_Try_turn_cancellation_into_a_failure()
    {
        ResultConfiguration.Configure(options => options.ShouldCatch = _ => true);

        var result = Result.Try((Action)(() => throw new OperationCanceledException()));

        Assert.Equal(ErrorKind.Cancelled, result.GetError().Kind);
    }

    [Fact]
    internal void ShouldCatch_can_let_an_exception_escape()
    {
        ResultConfiguration.Configure(options => options.ShouldCatch = exception => exception is not InvalidOperationException);

        Assert.Multiple(
            () => Assert.Throws<InvalidOperationException>(() => Result.Try((Action)(() => throw new InvalidOperationException()))),
            () => Assert.True(Result.Try((Action)(() => throw new FormatException())).IsFailure));
    }

    [Fact]
    [SuppressMessage("Usage", "xUnit1051", Justification = "The default (none) cancellation token is part of what these tests exercise.")]
    internal async Task DefaultMaxDegreeOfParallelism_limits_the_parallel_helpers_when_the_caller_passes_no_limit()
    {
        ResultConfiguration.Configure(options => options.DefaultMaxDegreeOfParallelism = 2);
        var current = 0;
        var highest = 0;

        var result = await Enumerable.Range(1, 12).SelectInParallel(
            async (item, token) =>
            {
                var now = Interlocked.Increment(ref current);
                InterlockedMax(ref highest, now);
                await Task.Delay(10, token);
                Interlocked.Decrement(ref current);

                return Result.Success(item);
            });

        Assert.Multiple(
            () => Assert.True(result.IsSuccess),
            () => Assert.InRange(highest, 1, 2));
    }

    [Fact]
    [SuppressMessage("Usage", "xUnit1051", Justification = "The default (none) cancellation token is part of what these tests exercise.")]
    internal async Task An_explicit_limit_wins_over_DefaultMaxDegreeOfParallelism()
    {
        ResultConfiguration.Configure(options => options.DefaultMaxDegreeOfParallelism = 1);
        var current = 0;
        var highest = 0;

        var result = await Enumerable.Range(1, 12).SelectInParallel(
            async (item, token) =>
            {
                var now = Interlocked.Increment(ref current);
                InterlockedMax(ref highest, now);
                await Task.Delay(10, token);
                Interlocked.Decrement(ref current);

                return Result.Success(item);
            },
            maxDegreeOfParallelism: 4);

        Assert.Multiple(
            () => Assert.True(result.IsSuccess),
            () => Assert.InRange(highest, 2, 4));
    }

    [Fact]
    internal void Reset_restores_every_default()
    {
        ResultConfiguration.Configure(options =>
        {
            options.ChildSeparator = " | ";
            options.CancelledMessage = "Stopped.";
        });

        ResultConfiguration.Reset();

        Assert.Multiple(
            () => Assert.Equal("A., B.", Error.Aggregate([Error.Create("A."), Error.Create("B.")]).Message),
            () => Assert.Equal("The operation was cancelled.", Error.Cancelled().Message));
    }

    [Fact]
    internal void Configure_changes_nothing_when_the_action_throws()
    {
        Assert.Throws<InvalidOperationException>(() => ResultConfiguration.Configure(options =>
        {
            options.CancelledMessage = "Stopped.";

            throw new InvalidOperationException();
        }));

        Assert.Equal("The operation was cancelled.", Error.Cancelled().Message);
    }

    [Fact]
    internal void Changing_the_options_after_Configure_has_returned_changes_nothing()
    {
        ResultOptions? kept = null;
        ResultConfiguration.Configure(options => kept = options);

        kept!.CancelledMessage = "Sneaky.";

        Assert.Equal("The operation was cancelled.", Error.Cancelled().Message);
    }

    [Fact]
    internal void Invalid_values_are_rejected()
    {
        ResultConfiguration.Configure(options => Assert.Multiple(
            () => Assert.Throws<ArgumentNullException>(() => options.ChildSeparator = null!),
            () => Assert.Throws<ArgumentNullException>(() => options.ContextSeparator = null!),
            () => Assert.Throws<ArgumentNullException>(() => options.RedactedValue = null!),
            () => Assert.Throws<ArgumentNullException>(() => options.ExceptionMapper = null!),
            () => Assert.Throws<ArgumentNullException>(() => options.ShouldCatch = null!),
            () => Assert.Throws<ArgumentException>(() => options.AggregateMessage = " "),
            () => Assert.Throws<ArgumentException>(() => options.CancelledMessage = string.Empty),
            () => Assert.Throws<ArgumentOutOfRangeException>(() => options.DefaultMaxDegreeOfParallelism = 0),
            () => Assert.Throws<ArgumentOutOfRangeException>(() => options.DefaultMaxDegreeOfParallelism = -2)));
    }

    [Fact]
    internal void Configure_throws_when_the_action_is_null()
    {
        Assert.Throws<ArgumentNullException>(() => ResultConfiguration.Configure(null!));
    }

    [Fact]
    internal async Task The_async_methods_do_not_continue_on_the_captured_context_by_default()
    {
        var posts = await CountPostsOfMapOnPendingTaskAsync();

        Assert.Equal(0, posts);
    }

    [Fact]
    internal async Task ContinueOnCapturedContext_makes_the_async_methods_continue_on_the_captured_context()
    {
        ResultConfiguration.Configure(options => options.ContinueOnCapturedContext = true);

        var posts = await CountPostsOfMapOnPendingTaskAsync();

        Assert.Equal(1, posts);
    }

    [Fact]
    internal async Task A_typed_Try_does_not_continue_on_the_captured_context_by_default()
    {
        var posts = await CountPostsOfTypedTryOnPendingTaskAsync();

        Assert.Equal(0, posts);
    }

    [Fact]
    internal async Task ContinueOnCapturedContext_makes_a_typed_Try_continue_on_the_captured_context()
    {
        ResultConfiguration.Configure(options => options.ContinueOnCapturedContext = true);

        var posts = await CountPostsOfTypedTryOnPendingTaskAsync();

        Assert.Equal(1, posts);
    }

    private static async Task<int> CountPostsOfTypedTryOnPendingTaskAsync()
    {
        var previous = SynchronizationContext.Current;
        var context = new CountingSynchronizationContext();
        SynchronizationContext.SetSynchronizationContext(context);

        try
        {
            var source = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
            var pending = Result.Try(() => source.Task, (InvalidOperationException exception) => Error.FromException(exception));
            source.SetResult(1);
            await pending.ConfigureAwait(false);

            return context.Posts;
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(previous);
        }
    }

    private static async Task<int> CountPostsOfMapOnPendingTaskAsync()
    {
        var previous = SynchronizationContext.Current;
        var context = new CountingSynchronizationContext();
        SynchronizationContext.SetSynchronizationContext(context);

        try
        {
            var source = new TaskCompletionSource<Result<int>>(TaskCreationOptions.RunContinuationsAsynchronously);
            var mapped = source.Task.Map(value => value + 1);
            source.SetResult(Result.Success(1));
            await mapped.ConfigureAwait(false);

            return context.Posts;
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(previous);
        }
    }

    private static void InterlockedMax(ref int target, int value)
    {
        int current;

        do
        {
            current = Volatile.Read(ref target);
        }
        while (value > current && Interlocked.CompareExchange(ref target, value, current) != current);
    }
}
