namespace Wyzely.Functional.ResultTests;

// The generated matrix tests cover every receiver and delegate variant of ValueOr; these tests cover how a call binds to them from consumer code.
public sealed class ValueOrTests
{
    private static readonly Error Boom = Error.Create("Boom.");

    [Fact]
    internal void A_lambda_without_parameters_computes_the_fallback_of_a_failed_result()
    {
        var outcome = Result.Failure<int>(Boom).ValueOr(static () => 5);

        Assert.Equal(5, outcome);
    }

    [Fact]
    internal void A_lambda_that_ignores_the_error_computes_the_fallback_of_a_failed_result()
    {
        var outcome = Result.Failure<int>(Boom).ValueOr(static _ => 6);

        Assert.Equal(6, outcome);
    }

    [Fact]
    internal void A_lambda_that_uses_the_error_computes_the_fallback_from_it()
    {
        var outcome = Result.Failure<int>(Boom).ValueOr(static error => error.Message.Length);

        Assert.Equal(5, outcome);
    }

    [Fact]
    internal void A_method_group_without_parameters_computes_the_fallback()
    {
        var outcome = Result.Failure<int>(Boom).ValueOr(Eight);

        Assert.Equal(8, outcome);
    }

    [Fact]
    internal async Task An_asynchronous_lambda_gives_a_task_of_the_fallback()
    {
        var outcome = await Result.Failure<int>(Boom).ValueOr(static async () =>
        {
            await Task.Yield();

            return 7;
        });

        Assert.Equal(7, outcome);
    }

    [Fact]
    internal async Task A_task_of_a_failed_result_takes_a_fallback_value()
    {
        var outcome = await Task.FromResult(Result.Failure<int>(Boom)).ValueOr(4);

        Assert.Equal(4, outcome);
    }

    [Fact]
    internal async Task A_value_task_of_a_failed_result_takes_a_fallback_value()
    {
        var outcome = await new ValueTask<Result<int>>(Result.Failure<int>(Boom)).ValueOr(2);

        Assert.Equal(2, outcome);
    }

    [Fact]
    internal async Task A_task_of_a_failed_result_takes_a_lambda_that_computes_the_fallback()
    {
        var outcome = await Task.FromResult(Result.Failure<int>(Boom)).ValueOr(static () => 3);

        Assert.Equal(3, outcome);
    }

    [Fact]
    internal void A_successful_result_gives_its_value_without_computing_the_fallback()
    {
        var calls = 0;

        var outcome = Result.Success(2).ValueOr(() =>
        {
            calls++;

            return 9;
        });

        Assert.Multiple(
            () => Assert.Equal(2, outcome),
            () => Assert.Equal(0, calls));
    }

    // The instance method ValueOr(T) wins when it applies. For a result of object a lambda without parameters has a natural type that converts to object,
    // so it is taken as the fallback value itself; a lambda with a parameter has no natural type and computes the fallback.
    [Fact]
    internal void For_a_result_of_object_a_lambda_without_parameters_is_taken_as_the_fallback_value()
    {
        var outcome = Result.Failure<object>(Boom).ValueOr(static () => 5);

        Assert.IsType<Func<int>>(outcome);
    }

    [Fact]
    internal void For_a_result_of_object_a_lambda_with_a_parameter_computes_the_fallback()
    {
        var outcome = Result.Failure<object>(Boom).ValueOr(static _ => 5);

        Assert.Equal(5, outcome);
    }

    private static int Eight() => 8;
}
