namespace Wyzely.Functional.ResultTests;

// A result that was never assigned is a success: default(Result) is Result.Success() and default(Result<T>) is Result.Success(default(T)).
// This is what makes every member work on every value, so there is no third state to check for or to throw about.
public sealed class DefaultResultTests
{
    [Fact]
    internal void A_default_result_is_the_same_as_Result_Success()
    {
        Result defaulted = default;

        Assert.Equal(Result.Success(), defaulted);
    }

    [Fact]
    internal void A_default_typed_result_is_the_same_as_a_success_with_the_default_value()
    {
        Result<int> defaulted = default;
        Result<string?> defaultedText = default;

        Assert.Multiple(
            () => Assert.Equal(Result.Success(0), defaulted),
            () => Assert.Equal(Result.Success<string?>(null), defaultedText));
    }

    [Fact]
    internal void A_result_in_an_unassigned_field_is_a_success()
    {
        (Result Plain, Result<int> Typed) unassigned = default;

        Assert.Multiple(
            () => Assert.True(unassigned.Plain.IsSuccess),
            () => Assert.Equal(0, unassigned.Typed.GetValue()));
    }

    [Fact]
    internal void A_failure_is_never_mistaken_for_the_default()
    {
        Result plain = default;
        Result<int> typed = default;

        Assert.Multiple(
            () => Assert.NotEqual(plain, Result.Failure(Error.Create("Boom."))),
            () => Assert.NotEqual(typed, Result.Failure<int>(Error.Create("Boom."))));
    }

    [Fact]
    internal void Every_member_works_on_a_default_result_without_throwing()
    {
        Result<int> typed = default;
        Result plain = default;

        Assert.Multiple(
            () => Assert.Equal(1, typed.Map(static x => x + 1).GetValue()),
            () => Assert.Equal(0, typed.Bind(static x => Result.Success(x)).GetValue()),
            () => Assert.True(typed.Ensure(static x => x == 0, Error.Create("Not zero.")).IsSuccess),
            () => Assert.Equal("ok", typed.Match(static _ => "ok", static _ => "failed")),
            () => Assert.Equal("ok", plain.Match(static () => "ok", static _ => "failed")));
    }
}
