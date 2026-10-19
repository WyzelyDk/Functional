namespace Wyzely.Functional.Tests;

// Maybe and Result in one chain: the point of the umbrella package.
public sealed class InteropChainTests
{
    [Fact]
    internal async Task A_found_value_travels_from_maybe_to_result_in_one_chain()
    {
        var outcome = await FindEmailAsync(1)
            .Filter(static email => email.Length > 3)
            .ToResult(Error.NotFound("No such customer."))
            .Bind(Validate)
            .Map(static email => email.ToUpperInvariant());

        Assert.Equal("ADA@EXAMPLE.COM", outcome.GetValue());
    }

    [Fact]
    internal async Task A_missing_value_becomes_the_error_you_chose()
    {
        var outcome = await FindEmailAsync(2)
            .ToResult(Error.NotFound("No such customer."))
            .Bind(Validate);

        Assert.Equal(ErrorKind.NotFound, outcome.GetError().Kind);
    }

    [Fact]
    internal async Task A_result_can_continue_as_a_maybe()
    {
        var name = await Task.FromResult(Result.Success("ada"))
            .ToMaybe()
            .Map(static value => value.ToUpperInvariant())
            .ValueOr(static () => "unknown");

        Assert.Equal("ADA", name);
    }

    private static Task<Maybe<string>> FindEmailAsync(int id) =>
        Task.FromResult(id == 1 ? Maybe.Some("ada@example.com") : Maybe.None<string>());

    private static Result<string> Validate(string email) =>
        email.Contains('@', StringComparison.Ordinal) ? Result.Success(email) : Result.Failure<string>("Not an email address.");
}
