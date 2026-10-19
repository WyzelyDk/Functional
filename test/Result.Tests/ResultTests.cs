using System.Diagnostics.CodeAnalysis;

namespace Wyzely.Functional.ResultTests;

public sealed class ResultTests
{
    private static readonly Error Boom = Error.Create("Boom.");

    [Fact]
    internal void Success_returns_a_successful_result()
    {
        var result = Result.Success();

        Assert.Multiple(
            () => Assert.True(result.IsSuccess),
            () => Assert.False(result.IsFailure));
    }

    [Fact]
    internal void Failure_returns_a_failed_result_with_the_error()
    {
        var result = Result.Failure(Boom);

        Assert.Multiple(
            () => Assert.False(result.IsSuccess),
            () => Assert.True(result.IsFailure),
            () => Assert.Same(Boom, result.GetError()));
    }

    [Fact]
    internal void Failure_with_a_message_creates_an_error_with_that_message()
    {
        var result = Result.Failure("Nope.");

        Assert.Equal("Nope.", result.GetError().Message);
    }

    [Fact]
    internal void Failure_throws_when_the_error_is_null()
    {
        Assert.Throws<ArgumentNullException>(() => Result.Failure((Error)null!));
    }

    [Fact]
    internal void Failure_throws_when_the_message_is_null()
    {
        Assert.Throws<ArgumentNullException>(() => Result.Failure((string)null!));
    }

    [Fact]
    internal void FromException_returns_a_failed_result_with_an_error_for_the_exception()
    {
        var exception = new InvalidOperationException("boom");

        var result = Result.FromException(exception);

        Assert.Multiple(
            () => Assert.True(result.IsFailure),
            () => Assert.Same(exception, result.GetError().Exception));
    }

    [Fact]
    internal void SuccessIf_returns_success_when_the_condition_holds_and_failure_otherwise()
    {
        Assert.Multiple(
            () => Assert.True(Result.SuccessIf(true, Boom).IsSuccess),
            () => Assert.Same(Boom, Result.SuccessIf(false, Boom).GetError()));
    }

    [Fact]
    internal void FailureIf_returns_failure_when_the_condition_holds_and_success_otherwise()
    {
        Assert.Multiple(
            () => Assert.Same(Boom, Result.FailureIf(true, Boom).GetError()),
            () => Assert.True(Result.FailureIf(false, Boom).IsSuccess));
    }

    [Fact]
    internal void SuccessIf_with_a_message_creates_an_error_with_that_message_when_the_condition_does_not_hold()
    {
        Assert.Multiple(
            () => Assert.True(Result.SuccessIf(true, "Nope.").IsSuccess),
            () => Assert.Equal("Nope.", Result.SuccessIf(false, "Nope.").GetError().Message));
    }

    [Fact]
    internal void FailureIf_with_a_message_creates_an_error_with_that_message_when_the_condition_holds()
    {
        Assert.Multiple(
            () => Assert.Equal("Nope.", Result.FailureIf(true, "Nope.").GetError().Message),
            () => Assert.True(Result.FailureIf(false, "Nope.").IsSuccess));
    }

    [Fact]
    internal void SuccessIf_and_FailureIf_throw_when_the_message_is_null()
    {
        Assert.Multiple(
            () => Assert.Throws<ArgumentNullException>(() => Result.SuccessIf(true, (string)null!)),
            () => Assert.Throws<ArgumentNullException>(() => Result.FailureIf(false, (string)null!)));
    }

    [Fact]
    internal void Error_throws_when_the_result_succeeded()
    {
        Assert.Throws<InvalidOperationException>(() => Result.Success().GetError());
    }

    [Fact]
    internal void TryGetError_returns_the_error_of_a_failed_result()
    {
        var found = Result.Failure(Boom).TryGetError(out var error);

        Assert.Multiple(
            () => Assert.True(found),
            () => Assert.Same(Boom, error));
    }

    [Fact]
    internal void TryGetError_returns_false_for_a_successful_result()
    {
        var found = Result.Success().TryGetError(out var error);

        Assert.Multiple(
            () => Assert.False(found),
            () => Assert.Null(error));
    }

    [Fact]
    internal void ThrowIfFailure_returns_for_a_successful_result()
    {
        var exception = Record.Exception(() => Result.Success().ThrowIfFailure());

        Assert.Null(exception);
    }

    [Fact]
    internal void ThrowIfFailure_throws_with_the_error_for_a_failed_result()
    {
        var exception = Assert.Throws<ResultFailedException>(() => Result.Failure(Boom).ThrowIfFailure());

        Assert.Multiple(
            () => Assert.Same(Boom, exception.Error),
            () => Assert.Contains("Boom.", exception.Message, StringComparison.Ordinal));
    }

    [Fact]
    internal void ThrowIfFailure_uses_the_exception_of_the_error_as_the_inner_exception()
    {
        var cause = new InvalidOperationException("cause");

        var exception = Assert.Throws<ResultFailedException>(() => Result.FromException(cause).ThrowIfFailure());

        Assert.Same(cause, exception.InnerException);
    }

    [Fact]
    internal void A_default_result_is_a_success()
    {
        Result result = default;

        Assert.Multiple(
            () => Assert.True(result.IsSuccess),
            () => Assert.False(result.IsFailure),
            () => Assert.False(result.TryGetError(out _)),
            () => Assert.Throws<InvalidOperationException>(() => result.GetError()),
            result.ThrowIfFailure);
    }

    [Fact]
    [SuppressMessage("StyleCop.CSharp.ReadabilityRules", "SA1129", Justification = "The parameterless constructor of a struct is the case under test.")]
    internal void A_result_made_with_the_parameterless_constructor_is_a_success()
    {
        var result = new Result();

        Assert.True(result.IsSuccess);
    }

    [Fact]
    internal void A_default_result_is_printed_compared_and_hashed_like_a_success()
    {
        Result result = default;

        Assert.Multiple(
            () => Assert.Equal("Success", result.ToString()),
            () => Assert.True(result.Equals(Result.Success())),
            () => Assert.True(result == Result.Success()),
            () => Assert.False(result.Equals(Result.Failure(Boom))),
            () => Assert.Equal(Result.Success().GetHashCode(), result.GetHashCode()));
    }

    [Fact]
    internal void An_array_of_results_that_was_never_filled_holds_successes()
    {
        var results = new Result[2];

        Assert.All(results, static result => Assert.True(result.IsSuccess));
    }

    [Fact]
    internal void ToString_describes_the_state()
    {
        Assert.Multiple(
            () => Assert.Equal("Success", Result.Success().ToString()),
            () => Assert.Equal("Failure: Boom.", Result.Failure(Boom).ToString()));
    }

    [Fact]
    internal void Equality_compares_state_and_error()
    {
        Assert.Multiple(
            () => Assert.True(Result.Success().Equals(Result.Success())),
            () => Assert.True(Result.Success() == Result.Success()),
            () => Assert.Equal(Result.Success().GetHashCode(), Result.Success().GetHashCode()),
            () => Assert.True(Result.Failure(Boom) == Result.Failure(Error.Create("Boom."))),
            () => Assert.Equal(Result.Failure(Boom).GetHashCode(), Result.Failure(Error.Create("Boom.")).GetHashCode()),
            () => Assert.True(Result.Failure(Boom) != Result.Failure(Error.Create("Other."))),
            () => Assert.True(Result.Success() != Result.Failure(Boom)),
            () => Assert.True(Result.Success().Equals((object)Result.Success())),
            () => Assert.False(Result.Success().Equals("Success")));
    }
}
