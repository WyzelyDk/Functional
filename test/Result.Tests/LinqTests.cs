namespace Wyzely.Functional.ResultTests;

public sealed class LinqTests
{
    private static readonly Error Boom = Error.Create("Boom.");

    [Fact]
    internal void Select_transforms_the_value_of_a_successful_result()
    {
        var result = Result.Success(2).Select(x => x * 10);

        Assert.Equal(20, result.GetValue());
    }

    [Fact]
    internal void Select_keeps_the_error_of_a_failed_result()
    {
        var result = Result.Failure<int>(Boom).Select(x => x * 10);

        Assert.Same(Boom, result.GetError());
    }

    [Fact]
    internal void SelectMany_chains_an_operation_that_can_fail()
    {
        var result = Result.Success(2).SelectMany(x => Result.Success(x + 1));

        Assert.Equal(3, result.GetValue());
    }

    [Fact]
    internal void SelectMany_keeps_the_error_of_a_failed_result_and_skips_the_selector()
    {
        var calls = 0;

        var result = Result.Failure<int>(Boom).SelectMany(x =>
        {
            calls++;

            return Result.Success(x);
        });

        Assert.Multiple(
            () => Assert.Same(Boom, result.GetError()),
            () => Assert.Equal(0, calls));
    }

    [Fact]
    internal void SelectMany_with_a_result_selector_combines_both_values()
    {
        var result = Result.Success(2).SelectMany(x => Result.Success(x + 1), (x, y) => x * y);

        Assert.Equal(6, result.GetValue());
    }

    [Fact]
    internal void SelectMany_with_a_result_selector_returns_the_error_of_the_inner_result()
    {
        var result = Result.Success(2).SelectMany(x => Result.Failure<int>(Boom), (x, y) => x * y);

        Assert.Same(Boom, result.GetError());
    }

    [Fact]
    internal void Query_syntax_chains_results()
    {
        var result = from a in Result.Success(1)
                     from b in Result.Success(2)
                     select a + b;

        Assert.Equal(3, result.GetValue());
    }

    [Fact]
    internal void Query_syntax_stops_at_the_first_failure()
    {
        var result = from a in Result.Success(1)
                     from b in Result.Failure<int>(Boom)
                     from c in Result.Success(3)
                     select a + b + c;

        Assert.Same(Boom, result.GetError());
    }

    [Fact]
    internal void Zip_combines_the_values_of_two_successful_results()
    {
        var result = Result.Success(1).Zip(Result.Success("a"));

        Assert.Equal((1, "a"), result.GetValue());
    }

    [Fact]
    internal void Zip_returns_the_error_of_the_one_that_failed()
    {
        Assert.Multiple(
            () => Assert.Same(Boom, Result.Failure<int>(Boom).Zip(Result.Success("a")).GetError()),
            () => Assert.Same(Boom, Result.Success(1).Zip(Result.Failure<string>(Boom)).GetError()));
    }

    [Fact]
    internal void Zip_aggregates_both_errors_when_both_failed()
    {
        var other = Error.Create("Other.");

        var result = Result.Failure<int>(Boom).Zip(Result.Failure<string>(other));

        Assert.Equal([Boom, other], result.GetError().Children);
    }

    [Fact]
    internal void Zip_with_a_selector_combines_the_values()
    {
        var result = Result.Success(2).Zip(Result.Success(3), (a, b) => a * b);

        Assert.Equal(6, result.GetValue());
    }

    [Fact]
    internal void Zip_with_a_selector_throws_when_the_selector_is_null()
    {
        Assert.Throws<ArgumentNullException>(() => Result.Success(2).Zip(Result.Success(3), (Func<int, int, int>)null!));
    }
}
