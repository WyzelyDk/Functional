namespace Wyzely.Functional.ResultTests;

// GetValueOrThrow and ThrowIfFailure are the explicit ways to leave the railway with an exception. The matrix tests cover every receiver; these pin
// where the exception comes out for a task, and the exceptions of the factory.
public sealed class ThrowingStepsTests
{
    private static readonly Error Rejected = Error.Create("Rejected.");

    [Fact]
    internal void GetValueOrThrow_of_a_completed_task_faults_the_task_instead_of_throwing_from_the_call()
    {
        var receiver = Task.FromResult(Result.Failure<int>(Rejected));

        var task = receiver.GetValueOrThrow();

        Assert.Multiple(
            () => Assert.True(task.IsFaulted),
            () => Assert.IsType<ResultFailedException>(task.Exception?.InnerException));
    }

    [Fact]
    internal void GetValueOrThrow_with_a_factory_of_a_completed_task_faults_the_task_instead_of_throwing_from_the_call()
    {
        var receiver = Task.FromResult(Result.Failure<int>(Rejected));

        var task = receiver.GetValueOrThrow(static error => new FormatException(error.Message));

        Assert.IsType<FormatException>(task.Exception?.InnerException);
    }

    [Fact]
    internal void ThrowIfFailure_of_a_completed_value_task_faults_the_task_instead_of_throwing_from_the_call()
    {
        var receiver = new ValueTask<Result>(Result.Failure(Rejected));

        var task = receiver.ThrowIfFailure().AsTask();

        Assert.IsType<ResultFailedException>(task.Exception?.InnerException);
    }

    [Fact]
    internal void GetValueOrThrow_returns_the_value_of_a_completed_successful_task_in_a_completed_task()
    {
        var task = Task.FromResult(Result.Success(2)).GetValueOrThrow();

        Assert.Multiple(
            () => Assert.True(task.IsCompletedSuccessfully),
            () => Assert.Equal(2, task.Result));
    }

    [Fact]
    internal void GetValueOrThrow_throws_the_exception_of_the_factory_for_a_failed_result()
    {
        var result = Result.Failure<int>(Rejected);

        var exception = Record.Exception(() => result.GetValueOrThrow(static error => new FormatException(error.Message)));

        Assert.Equal("Rejected.", Assert.IsType<FormatException>(exception).Message);
    }

    [Fact]
    internal void GetValueOrThrow_returns_the_value_of_a_successful_result_without_calling_the_factory()
    {
        var calls = 0;

        var value = Result.Success(2).GetValueOrThrow(_ =>
        {
            calls++;

            return new FormatException();
        });

        Assert.Multiple(
            () => Assert.Equal(2, value),
            () => Assert.Equal(0, calls));
    }

    [Fact]
    internal void GetValueOrThrow_throws_when_the_factory_returns_null()
    {
        var result = Result.Failure<int>(Rejected);

        var exception = Record.Exception(() => result.GetValueOrThrow(static _ => null!));

        Assert.Equal("exceptionFactory", Assert.IsType<ArgumentNullException>(exception).ParamName);
    }

    [Fact]
    internal void GetValueOrThrow_throws_when_the_factory_is_null()
    {
        var result = Result.Success(2);

        var exception = Record.Exception(() => result.GetValueOrThrow(null!));

        Assert.Equal("exceptionFactory", Assert.IsType<ArgumentNullException>(exception).ParamName);
    }

    [Fact]
    internal void ThrowIfFailure_with_a_factory_throws_the_exception_of_the_factory()
    {
        var result = Result.Failure(Rejected);

        var exception = Record.Exception(() => result.ThrowIfFailure(static error => new FormatException(error.Message)));

        Assert.IsType<FormatException>(exception);
    }

    [Fact]
    internal void ThrowIfFailure_with_a_factory_does_nothing_for_a_successful_result()
    {
        var result = Result.Success();

        var exception = Record.Exception(() => result.ThrowIfFailure(static _ => new FormatException()));

        Assert.Null(exception);
    }

    [Fact]
    internal async Task GetValueOrThrow_awaits_a_receiver_that_completes_later()
    {
        var value = await Pending.Of(Result.Success(2)).GetValueOrThrow();

        Assert.Equal(2, value);
    }

    [Fact]
    internal async Task GetValueOrThrow_ends_a_chain_without_an_await_in_the_middle()
    {
        var value = await Task.FromResult(Result.Success(2))
            .Map(static number => number * 10)
            .Ensure(static number => number > 5, Rejected)
            .GetValueOrThrow();

        Assert.Equal(20, value);
    }
}
