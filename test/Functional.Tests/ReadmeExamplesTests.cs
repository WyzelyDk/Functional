namespace Wyzely.Functional.Tests;

// Every code sample of the Wyzely.Functional README and of docs/functional/interop.md is a test here, so the documentation cannot silently rot.
public sealed class ReadmeExamplesTests
{
    // README: the example at the top.
    [Fact]
    internal async Task ReadmeTopExample_charges_a_found_customer()
    {
        var outcome = await FindCustomerAsync(7)
            .ToResult(Error.NotFound("No such customer."))
            .Bind(ChargeAsync)
            .Map(receipt => receipt.Number);

        Assert.Equal("R-7", outcome.GetValue());
    }

    [Fact]
    internal async Task ReadmeTopExample_fails_with_not_found_when_the_customer_is_unknown()
    {
        var outcome = await FindCustomerAsync(8)
            .ToResult(Error.NotFound("No such customer."))
            .Bind(ChargeAsync)
            .Map(receipt => receipt.Number);

        Assert.Multiple(
            () => Assert.True(outcome.IsFailure),
            () => Assert.Equal(ErrorKind.NotFound, outcome.GetError().Kind));
    }

    // README: the interop methods, and docs/functional/interop.md: ToResult and ToMaybe.
    [Fact]
    internal void ReadmeEnsureSomeExample_turns_a_result_of_a_maybe_into_a_result_of_the_value()
    {
        var lookup = Result.Success(Maybe.None<string>());

        var customer = lookup.EnsureSome(Error.NotFound("No such customer."));

        Assert.Equal("No such customer.", customer.GetError().Message);
    }

    [Fact]
    internal void ReadmeInteropExample_converts_in_both_directions()
    {
        var found = Maybe.Some("ada@example.com");
        var missing = Maybe.None<string>();

        var success = found.ToResult(Error.NotFound("No such customer."));
        var failure = missing.ToResult(() => Error.NotFound("No such customer."));

        var someValue = Result.Success(5).ToMaybe();
        var noValue = Result.Failure<int>("It broke.").ToMaybe();

        Assert.Multiple(
            () => Assert.Equal("ada@example.com", success.GetValue()),
            () => Assert.Equal(ErrorKind.NotFound, failure.GetError().Kind),
            () => Assert.Equal(Maybe.Some(5), someValue),
            () => Assert.True(noValue.IsNone));
    }

    [Fact]
    internal void InteropPage_ToResult_snippet_gives_the_error_you_gave_and_creates_a_lazy_error_only_for_none()
    {
        var found = Maybe.Some("ada@example.com");
        var missing = Maybe.None<string>();
        var created = 0;

        var success = found.ToResult(Error.NotFound("No such customer."));
        var failure = missing.ToResult(Error.NotFound("No such customer."));
        var lazy = missing.ToResult(() =>
        {
            created++;
            return Error.NotFound("No such customer.");
        });
        var notCalled = found.ToResult(() =>
        {
            created++;
            return Error.NotFound("No such customer.");
        });

        Assert.Multiple(
            () => Assert.Equal("ada@example.com", success.GetValue()),
            () => Assert.Equal("No such customer.", failure.GetError().Message),
            () => Assert.Equal("No such customer.", lazy.GetError().Message),
            () => Assert.True(notCalled.IsSuccess),
            () => Assert.Equal(1, created));
    }

    [Fact]
    internal void InteropPage_ToResult_throws_when_the_error_or_the_factory_is_null_or_returns_null()
    {
        var missing = Maybe.None<string>();

        Assert.Multiple(
            () => Assert.Throws<ArgumentNullException>(() => missing.ToResult((Error)null!)),
            () => Assert.Throws<ArgumentNullException>(() => missing.ToResult((Func<Error>)null!)),
            () => Assert.Throws<ArgumentNullException>(() => missing.ToResult(() => null!)));
    }

    [Fact]
    internal void InteropPage_ToMaybe_snippet_drops_the_error_and_turns_a_null_success_into_none()
    {
        var someValue = Result.Success(5).ToMaybe();
        var noValue = Result.Failure<int>("It broke.").ToMaybe();
        var nullValue = Result.Success<string>(null!).ToMaybe();

        Assert.Multiple(
            () => Assert.Equal(Maybe.Some(5), someValue),
            () => Assert.True(noValue.IsNone),
            () => Assert.True(nullValue.IsNone));
    }

    [Fact]
    internal void InteropPage_ToMaybe_of_a_default_result_is_some_of_the_default_value()
    {
        Assert.Equal(0, default(Result<int>).ToMaybe().GetValue());
    }

    // Root README: "Why a library, when writing your own Result and Maybe is easy?" shows these two types, so they must compile and behave.
    [Fact]
    internal void RootReadme_hand_written_types_compile_and_behave_as_described()
    {
        var ok = SimpleResult<int>.Ok(3);
        var fail = SimpleResult<int>.Fail("no");
        var some = SimpleMaybe<string>.Some("a");

        Assert.Multiple(
            () => Assert.True(ok.IsSuccess),
            () => Assert.False(fail.IsSuccess),
            () => Assert.True(some.HasValue),
            () => Assert.False(SimpleMaybe<string>.None.HasValue));
    }

    [Fact]
    internal async Task InteropPage_async_chain_charges_a_found_customer_and_keeps_the_error_for_an_unknown_one()
    {
        var charged = await FindCustomerAsync(7)
            .ToResult(Error.NotFound("No such customer."))
            .Bind(ChargeAsync)
            .Map(receipt => receipt.Number);
        var unknown = await FindCustomerAsync(9)
            .ToResult(Error.NotFound("No such customer."))
            .Bind(ChargeAsync)
            .Map(receipt => receipt.Number);

        Assert.Multiple(
            () => Assert.Equal("R-7", charged.GetValue()),
            () => Assert.Equal("No such customer.", unknown.GetError().Message));
    }

    [Fact]
    internal void InteropPage_ToInvertedResult_snippet_fails_for_a_value_and_succeeds_for_none()
    {
        var taken = Maybe.Some("ada@example.com");
        var free = Maybe.None<string>();

        var refused = taken.ToInvertedResult(Error.Conflict("The email is already registered."));
        var allowed = free.ToInvertedResult(Error.Conflict("The email is already registered."));
        var detailed = taken.ToInvertedResult(email => Error.Conflict($"{email} is already registered."));

        Assert.Multiple(
            () => Assert.Equal("The email is already registered.", refused.GetError().Message),
            () => Assert.True(allowed.IsSuccess),
            () => Assert.Equal("ada@example.com is already registered.", detailed.GetError().Message));
    }

    [Fact]
    internal async Task InteropPage_ToInvertedResult_chain_runs_the_creation_only_when_nobody_had_the_email()
    {
        var creations = new List<string>();

        async Task<Result> Create(string email)
        {
            await Task.Yield();
            creations.Add(email);

            return Result.Success();
        }

        static Task<Maybe<string>> FindUserAsync(string email) => Task.FromResult(email == "ada@example.com" ? Maybe.Some(email) : Maybe.None<string>());

        var refused = await FindUserAsync("ada@example.com")
            .ToInvertedResult(user => Error.Conflict($"{user} is already registered."))
            .Bind(() => Create("ada@example.com"));

        var created = await FindUserAsync("new@example.com")
            .ToInvertedResult(user => Error.Conflict($"{user} is already registered."))
            .Bind(() => Create("new@example.com"));

        Assert.Multiple(
            () => Assert.True(refused.IsFailure),
            () => Assert.True(created.IsSuccess),
            () => Assert.Equal(["new@example.com"], creations));
    }

    private static Task<Maybe<Customer>> FindCustomerAsync(int id) =>
        Task.FromResult(id == 7 ? Maybe.Some(new Customer(id)) : Maybe.None<Customer>());

    private static ValueTask<Result<Receipt>> ChargeAsync(Customer customer) => new(Result.Success(new Receipt("R-" + customer.Id)));

    private readonly record struct SimpleResult<T>(T? Value, string? Error)
    {
        public bool IsSuccess => Error is null;

        public static SimpleResult<T> Ok(T value) => new(value, null);

        public static SimpleResult<T> Fail(string error) => new(default, error);
    }

    private readonly record struct SimpleMaybe<T>(T? Value, bool HasValue)
        where T : notnull
    {
        public static SimpleMaybe<T> None => default;

        public static SimpleMaybe<T> Some(T value) => new(value, true);
    }

    private sealed record Customer(int Id);

    private sealed record Receipt(string Number);
}
