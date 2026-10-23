namespace Wyzely.Functional.ResultTests;

// BindZip keeps the value of a step next to the value of the next one, and Map and Bind take the elements of a tuple as separate parameters.
// The matrix tests cover every combination of receiver and delegate; these pin how they chain, which overload a call binds to, and the element names.
public sealed class TupleStepsTests
{
    private static readonly Error Rejected = Error.Create("Rejected.");

    [Fact]
    internal void BindZip_keeps_the_value_of_the_receiver_next_to_the_value_of_the_delegate()
    {
        var outcome = Result.Success(2).BindZip(static value => Result.Success(value.ToString(System.Globalization.CultureInfo.InvariantCulture)));

        Assert.Equal((2, "2"), outcome.GetValue());
    }

    [Fact]
    internal void BindZip_names_the_elements_of_the_tuple_like_Zip_does()
    {
        var outcome = Result.Success(2).BindZip(static value => Result.Success(value * 10)).Map(static pair => pair.First + pair.Second);

        Assert.Equal(22, outcome.GetValue());
    }

    [Fact]
    internal void BindZip_chains_up_to_five_values()
    {
        var outcome = Result.Success(1)
            .BindZip(static _ => Result.Success(2))
            .BindZip(static _ => Result.Success(3))
            .BindZip(static _ => Result.Success(4))
            .BindZip(static _ => Result.Success(5));

        Assert.Equal((1, 2, 3, 4, 5), outcome.GetValue());
    }

    [Fact]
    internal void BindZip_gives_the_whole_tuple_to_the_delegate_of_the_next_step()
    {
        var outcome = Result.Success(1)
            .BindZip(static _ => Result.Success(2))
            .BindZip(static pair => Result.Success(pair.Item1 + pair.Item2));

        Assert.Equal((1, 2, 3), outcome.GetValue());
    }

    [Fact]
    internal void BindZip_passes_a_failure_of_any_step_through()
    {
        var calls = 0;

        var outcome = Result.Success(1)
            .BindZip(static _ => Result.Failure<int>(Rejected))
            .BindZip(_ =>
            {
                calls++;

                return Result.Success(3);
            });

        Assert.Multiple(
            () => Assert.Same(Rejected, outcome.GetError()),
            () => Assert.Equal(0, calls));
    }

    [Fact]
    internal void BindZip_does_not_call_the_delegate_when_the_receiver_failed()
    {
        var calls = 0;

        var outcome = Result.Failure<int>(Rejected).BindZip(_ =>
        {
            calls++;

            return Result.Success(2);
        });

        Assert.Multiple(
            () => Assert.Same(Rejected, outcome.GetError()),
            () => Assert.Equal(0, calls));
    }

    [Fact]
    internal async Task BindZip_awaits_an_async_delegate_and_returns_a_task()
    {
        var task = AsTask(Result.Success(2).BindZip(static async value =>
        {
            await Task.Yield();

            return Result.Success(value * 10);
        }));

        var outcome = await task;

        Assert.Equal((2, 20), outcome.GetValue());
    }

    [Fact]
    internal async Task BindZip_accepts_a_task_receiver_and_a_value_task_delegate()
    {
        var outcome = await Task.FromResult(Result.Success(2)).BindZip(static value => new ValueTask<Result<int>>(Result.Success(value * 10)));

        Assert.Equal((2, 20), outcome.GetValue());
    }

    [Fact]
    internal void Map_takes_the_values_of_a_tuple_as_separate_parameters()
    {
        var outcome = Result.Success(1).Zip(Result.Success(2)).Map(static (first, second) => first + second);

        Assert.Equal(3, outcome.GetValue());
    }

    [Fact]
    internal void Bind_takes_the_values_of_a_tuple_as_separate_parameters()
    {
        var outcome = Result.Success(1).Zip(Result.Success(2)).Bind(static (first, second) => Result.Success((first * 10) + second));

        Assert.Equal(12, outcome.GetValue());
    }

    [Fact]
    internal void Map_with_a_lambda_of_one_parameter_still_gets_the_whole_tuple()
    {
        var outcome = Result.Success((1, 2)).Map(static pair => pair.Item1 + pair.Item2);

        Assert.Equal(3, outcome.GetValue());
    }

    [Fact]
    internal void Map_with_separate_parameters_works_up_to_five_values()
    {
        var outcome = Result.Success((1, 2, 3, 4, 5)).Map(static (a, b, c, d, e) => a + b + c + d + e);

        Assert.Equal(15, outcome.GetValue());
    }

    [Fact]
    internal void Map_accepts_a_method_group_for_the_separate_parameters()
    {
        var outcome = Result.Success((3, 4)).Map(Add);

        Assert.Equal(7, outcome.GetValue());
    }

    [Fact]
    internal void Map_with_separate_parameters_keeps_the_error_of_a_failed_result()
    {
        var calls = 0;

        var outcome = Result.Failure<(int, int)>(Rejected).Map((first, second) =>
        {
            calls++;

            return first + second;
        });

        Assert.Multiple(
            () => Assert.Same(Rejected, outcome.GetError()),
            () => Assert.Equal(0, calls));
    }

    [Fact]
    internal async Task Bind_with_separate_parameters_awaits_an_async_lambda_and_returns_a_task()
    {
        var task = AsTask(Result.Success((1, 2)).Bind(static async (first, second) =>
        {
            await Task.Yield();

            return Result.Success(first + second);
        }));

        var outcome = await task;

        Assert.Equal(3, outcome.GetValue());
    }

    [Fact]
    internal async Task Map_with_separate_parameters_accepts_a_value_task_receiver()
    {
        var outcome = await new ValueTask<Result<(int, int)>>(Result.Success((1, 2))).Map(static (first, second) => first + second);

        Assert.Equal(3, outcome.GetValue());
    }

    [Fact]
    internal void BindZip_then_Map_with_separate_parameters_reads_without_a_tuple_in_sight()
    {
        var outcome = Result.Success(2)
            .BindZip(static value => Result.Success(value * 10))
            .Map(static (value, tenfold) => value + tenfold);

        Assert.Equal(22, outcome.GetValue());
    }

    // Passing the call through a parameter of an exact type pins the type the overload returns.
    private static Task<Result<TValue>> AsTask<TValue>(Task<Result<TValue>> task) => task;

    private static int Add(int first, int second) => first + second;
}
