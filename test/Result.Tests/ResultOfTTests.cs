namespace Wyzely.Functional.ResultTests;

public sealed class ResultOfTTests
{
    private static readonly Error Boom = Error.Create("Boom.");

    [Fact]
    internal void Success_returns_a_successful_result_with_the_value()
    {
        var result = Result.Success(42);

        Assert.Multiple(
            () => Assert.True(result.IsSuccess),
            () => Assert.False(result.IsFailure),
            () => Assert.Equal(42, result.GetValue()));
    }

    [Fact]
    internal void Success_accepts_a_null_value_of_a_reference_type()
    {
        var result = Result.Success<string?>(null);

        Assert.Multiple(
            () => Assert.True(result.IsSuccess),
            () => Assert.Null(result.GetValue()));
    }

    [Fact]
    internal void Failure_returns_a_failed_result_with_the_error()
    {
        var result = Result.Failure<int>(Boom);

        Assert.Multiple(
            () => Assert.True(result.IsFailure),
            () => Assert.Same(Boom, result.GetError()));
    }

    [Fact]
    internal void Failure_with_a_message_creates_an_error_with_that_message()
    {
        Assert.Equal("Nope.", Result.Failure<int>("Nope.").GetError().Message);
    }

    [Fact]
    internal void Failure_throws_when_the_error_is_null()
    {
        Assert.Throws<ArgumentNullException>(() => Result.Failure<int>((Error)null!));
    }

    [Fact]
    internal void FromException_returns_a_failed_result_with_an_error_for_the_exception()
    {
        var exception = new InvalidOperationException("boom");

        var result = Result.FromException<int>(exception);

        Assert.Same(exception, result.GetError().Exception);
    }

    [Fact]
    internal void Value_throws_with_the_error_when_the_result_failed()
    {
        var exception = Assert.Throws<ResultFailedException>(() => Result.Failure<int>(Boom).GetValue());

        Assert.Same(Boom, exception.Error);
    }

    [Fact]
    internal void Error_throws_when_the_result_succeeded()
    {
        Assert.Throws<InvalidOperationException>(() => Result.Success(1).GetError());
    }

    [Fact]
    internal void TryGetValue_returns_the_value_of_a_successful_result()
    {
        var found = Result.Success(7).TryGetValue(out var value);

        Assert.Multiple(
            () => Assert.True(found),
            () => Assert.Equal(7, value));
    }

    [Fact]
    internal void TryGetValue_returns_false_for_a_failed_result()
    {
        var found = Result.Failure<int>(Boom).TryGetValue(out var value);

        Assert.Multiple(
            () => Assert.False(found),
            () => Assert.Equal(0, value));
    }

    [Fact]
    internal void TryGetError_returns_the_error_of_a_failed_result_and_false_otherwise()
    {
        var failed = Result.Failure<int>(Boom).TryGetError(out var error);
        var succeeded = Result.Success(1).TryGetError(out var none);

        Assert.Multiple(
            () => Assert.True(failed),
            () => Assert.Same(Boom, error),
            () => Assert.False(succeeded),
            () => Assert.Null(none));
    }

    [Fact]
    internal void ValueOr_returns_the_value_or_the_fallback()
    {
        Assert.Multiple(
            () => Assert.Equal(5, Result.Success(5).ValueOr(9)),
            () => Assert.Equal(9, Result.Failure<int>(Boom).ValueOr(9)));
    }

    [Fact]
    internal void Deconstruct_returns_the_value_and_no_error_for_a_successful_result()
    {
        var (value, error) = Result.Success("a");

        Assert.Multiple(
            () => Assert.Equal("a", value),
            () => Assert.Null(error));
    }

    [Fact]
    internal void Deconstruct_returns_the_error_and_no_value_for_a_failed_result()
    {
        var (value, error) = Result.Failure<string>(Boom);

        Assert.Multiple(
            () => Assert.Null(value),
            () => Assert.Same(Boom, error));
    }

    [Fact]
    internal void ToResult_drops_the_value_and_keeps_the_state()
    {
        Assert.Multiple(
            () => Assert.True(Result.Success(1).ToResult().IsSuccess),
            () => Assert.Same(Boom, Result.Failure<int>(Boom).ToResult().GetError()));
    }

    [Fact]
    internal void A_typed_result_converts_implicitly_to_an_untyped_result()
    {
        Result untyped = Result.Failure<int>(Boom);

        Assert.Same(Boom, untyped.GetError());
    }

    [Fact]
    internal void A_default_result_is_a_success_with_the_default_value()
    {
        Result<int> result = default;

        Assert.Multiple(
            () => Assert.True(result.IsSuccess),
            () => Assert.False(result.IsFailure),
            () => Assert.Equal(0, result.GetValue()),
            () => Assert.True(result.TryGetValue(out var value) && value == 0),
            () => Assert.False(result.TryGetError(out _)),
            () => Assert.Equal(0, result.ValueOr(1)),
            () => Assert.True(result.ToResult().IsSuccess));
    }

    [Fact]
    internal void A_default_result_of_a_reference_type_is_a_success_holding_null()
    {
        Result<string?> result = default;

        Assert.Multiple(
            () => Assert.True(result.IsSuccess),
            () => Assert.Null(result.GetValue()));
    }

    [Fact]
    internal void An_array_of_results_that_was_never_filled_holds_successes_with_the_default_value()
    {
        var results = new Result<int>[2];

        Assert.All(results, static result => Assert.Equal(0, result.GetValue()));
    }

    [Fact]
    internal void A_default_result_is_printed_compared_and_hashed_like_a_success_with_the_default_value()
    {
        Result<int> result = default;

        Assert.Multiple(
            () => Assert.Equal("Success(0)", result.ToString()),
            () => Assert.True(result.Equals(Result.Success(0))),
            () => Assert.True(result == Result.Success(0)),
            () => Assert.False(result.Equals(Result.Success(1))),
            () => Assert.Equal(Result.Success(0).GetHashCode(), result.GetHashCode()));
    }

    [Fact]
    internal void ToString_describes_the_state()
    {
        Assert.Multiple(
            () => Assert.Equal("Success(5)", Result.Success(5).ToString()),
            () => Assert.Equal("Success(null)", Result.Success<string?>(null).ToString()),
            () => Assert.Equal("Failure: Boom.", Result.Failure<int>(Boom).ToString()));
    }

    [Fact]
    internal void Equality_compares_state_value_and_error()
    {
        Assert.Multiple(
            () => Assert.True(Result.Success(1).Equals(Result.Success(1))),
            () => Assert.True(Result.Success(1) == Result.Success(1)),
            () => Assert.Equal(Result.Success(1).GetHashCode(), Result.Success(1).GetHashCode()),
            () => Assert.True(Result.Success(1) != Result.Success(2)),
            () => Assert.True(Result.Success<string?>(null) == Result.Success<string?>(null)),
            () => Assert.True(Result.Failure<int>(Boom) == Result.Failure<int>(Error.Create("Boom."))),
            () => Assert.Equal(Result.Failure<int>(Boom).GetHashCode(), Result.Failure<int>(Error.Create("Boom.")).GetHashCode()),
            () => Assert.True(Result.Failure<int>(Boom) != Result.Failure<int>(Error.Create("Other."))),
            () => Assert.True(Result.Success(1) != Result.Failure<int>(Boom)),
            () => Assert.True(Result.Success(1).Equals((object)Result.Success(1))),
            () => Assert.False(Result.Success(1).Equals("1")));
    }

    [Fact]
    internal void A_failed_result_ignores_the_value_in_equality()
    {
        Assert.True(Result.Failure<int>(Boom) == Result.Failure<int>(Boom));
    }

    [Fact]
    internal void A_success_without_a_value_has_the_same_hash_code_as_a_default_result()
    {
        Assert.Equal(Result.Success<string?>(null).GetHashCode(), default(Result<string>).GetHashCode());
    }
}
