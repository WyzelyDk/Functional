namespace Wyzely.Functional.ResultTests;

// The generated matrix tests cover every receiver and delegate variant of OrElse; these tests cover how a call binds to them from consumer code.
public sealed class OrElseTests
{
    private static readonly Error Boom = Error.Create("Boom.");
    private static readonly Error Other = Error.Create("Other.");

    [Fact]
    internal void A_lambda_that_reads_the_error_and_returns_a_value_replaces_the_failure_with_a_success()
    {
        var outcome = Result.Failure<int>(Boom).OrElse(static error => error.Message.Length);

        Assert.Equal(5, outcome.GetValue());
    }

    [Fact]
    internal void A_lambda_without_parameters_that_returns_a_value_replaces_the_failure_with_a_success()
    {
        var outcome = Result.Failure<int>(Boom).OrElse(static () => 7);

        Assert.Equal(7, outcome.GetValue());
    }

    [Fact]
    internal void A_lambda_that_returns_a_result_gives_that_result()
    {
        var outcome = Result.Failure<int>(Boom).OrElse(static _ => Result.Success(6));

        Assert.Equal(6, outcome.GetValue());
    }

    [Fact]
    internal void A_lambda_without_parameters_that_returns_a_result_gives_that_result()
    {
        var outcome = Result.Failure<int>(Boom).OrElse(static () => Result.Success(8));

        Assert.Equal(8, outcome.GetValue());
    }

    [Fact]
    internal void A_lambda_that_returns_a_failed_result_gives_that_failure()
    {
        var outcome = Result.Failure<int>(Boom).OrElse(static _ => Result.Failure<int>(Other));

        Assert.Same(Other, outcome.GetError());
    }

    [Fact]
    internal void A_lambda_that_returns_an_error_gives_a_failure_with_that_error()
    {
        var outcome = Result.Failure<int>(Boom).OrElse(static _ => Other);

        Assert.Same(Other, outcome.GetError());
    }

    [Fact]
    internal void A_method_group_that_takes_the_error_and_returns_a_value_replaces_the_failure_with_a_success()
    {
        var outcome = Result.Failure<int>(Boom).OrElse(NineFrom);

        Assert.Equal(9, outcome.GetValue());
    }

    [Fact]
    internal void A_method_group_without_parameters_that_returns_a_value_replaces_the_failure_with_a_success()
    {
        var outcome = Result.Failure<int>(Boom).OrElse(Eight);

        Assert.Equal(8, outcome.GetValue());
    }

    [Fact]
    internal void A_method_group_that_takes_the_error_and_returns_a_result_gives_that_result()
    {
        var outcome = Result.Failure<int>(Boom).OrElse(TenFrom);

        Assert.Equal(10, outcome.GetValue());
    }

    [Fact]
    internal async Task An_asynchronous_lambda_that_returns_a_value_gives_a_task_of_a_success()
    {
        var outcome = await Result.Failure<int>(Boom).OrElse(static async _ =>
        {
            await Task.Yield();

            return 11;
        });

        Assert.Equal(11, outcome.GetValue());
    }

    [Fact]
    internal async Task An_asynchronous_lambda_that_returns_a_result_gives_a_task_of_that_result()
    {
        var outcome = await Result.Failure<int>(Boom).OrElse(static async _ =>
        {
            await Task.Yield();

            return Result.Success(12);
        });

        Assert.Equal(12, outcome.GetValue());
    }

    [Fact]
    internal async Task A_task_of_a_failed_result_takes_a_lambda_that_returns_a_value()
    {
        var outcome = await Task.FromResult(Result.Failure<int>(Boom)).OrElse(static _ => 4);

        Assert.Equal(4, outcome.GetValue());
    }

    [Fact]
    internal void A_successful_result_is_returned_without_running_the_delegate()
    {
        var calls = 0;

        var outcome = Result.Success(2).OrElse(() =>
        {
            calls++;

            return 9;
        });

        Assert.Multiple(
            () => Assert.Equal(2, outcome.GetValue()),
            () => Assert.Equal(0, calls));
    }

    [Fact]
    internal void An_untyped_failure_takes_a_lambda_that_returns_a_result()
    {
        var outcome = Result.Failure(Boom).OrElse(static _ => Result.Success());

        Assert.True(outcome.IsSuccess);
    }

    [Fact]
    internal void An_untyped_failure_takes_a_lambda_without_parameters_that_returns_a_result()
    {
        var outcome = Result.Failure(Boom).OrElse(static () => Result.Success());

        Assert.True(outcome.IsSuccess);
    }

    [Fact]
    internal void An_untyped_failure_takes_a_lambda_that_returns_an_error()
    {
        var outcome = Result.Failure(Boom).OrElse(static _ => Other);

        Assert.Same(Other, outcome.GetError());
    }

    // A lambda that returns an int converts to a function of object and to a function of Result<object>, so for a result of object it is ambiguous (CS0121)
    // and a typed function says which one is meant; the ambiguity is pinned in ConsumerBindingTests of the analyzer tests.
    [Fact]
    internal void For_a_result_of_object_a_function_of_object_gives_a_success_with_that_value()
    {
        Func<object> replacement = FiveAsObject;

        var outcome = Result.Failure<object>(Boom).OrElse(replacement);

        Assert.Equal(5, outcome.GetValue());
    }

    private static int NineFrom(Error error) => error.Message.Length + 4;

    private static int Eight() => 8;

    private static object FiveAsObject() => 5;

    private static Result<int> TenFrom(Error error) => Result.Success(error.Message.Length + 5);
}
