using System.Globalization;

namespace Wyzely.Functional.ResultTests;

// Every code sample of the README is a test here, so the documentation cannot silently rot.
public sealed class ReadmeExamplesTests
{
    [Fact]
    internal void The_quick_start_example_works()
    {
        var message = ParseAge("42")
            .Ensure(age => age >= 18, Error.Validation("Must be an adult."))
            .Map(age => $"Welcome, age {age}.")
            .Match(text => text, error => $"Rejected: {error}");

        var rejected = ParseAge("abc").Match(age => "ok", error => $"Rejected: {error}");

        Assert.Multiple(
            () => Assert.Equal("Welcome, age 42.", message),
            () => Assert.Equal("Rejected: 'abc' is not a number.", rejected));
    }

    [Fact]
    internal void The_conditional_steps_example_runs_each_step_only_when_its_condition_holds()
    {
        var isMember = true;
        var couponIsValid = false;

        var price = Result.Success(100)
            .MapIf(isMember, static value => value * 9 / 10)
            .BindIf(() => couponIsValid, static value => Result.Success(value - 5))
            .EnsureIf(static value => value > 50, static value => Result.SuccessIf(value < 1000, "Needs approval."));

        Assert.Equal(90, price.GetValue());
    }

    [Fact]
    internal void The_try_steps_example_turns_the_exception_of_a_delegate_into_a_failure()
    {
        var address = Result.Success("not a web address")
            .MapTry(static text => new Uri(text), static (UriFormatException exception) => Error.Validation("The address is not valid."));

        Assert.Equal("The address is not valid.", address.GetError().Message);
    }

    [Fact]
    internal void The_not_null_example_turns_a_nullable_result_into_a_plain_one()
    {
        var name = Result.Success<string?>(null)
            .EnsureNotNull(Error.NotFound("No such customer."));

        Assert.Equal("No such customer.", name.GetError().Message);
    }

    [Fact]
    internal async Task The_async_chain_example_mixes_task_and_value_task_steps()
    {
        var outcome = await FindOrderAsync(7)
            .Ensure(order => order.IsOpen, Error.Conflict("The order is closed."))
            .Bind(ChargeAsync)
            .Map(receipt => receipt.Number)
            .MapError(error => error.WithContext("Checkout"));

        var closed = await FindOrderAsync(8)
            .Ensure(order => order.IsOpen, Error.Conflict("The order is closed."))
            .Bind(ChargeAsync)
            .MapError(error => error.WithContext("Checkout"));

        Assert.Multiple(
            () => Assert.Equal("R-7", outcome.GetValue()),
            () => Assert.Equal("Checkout: The order is closed.", closed.GetError().ToString()));
    }

    [Fact]
    internal void The_error_example_renders_templates_details_and_context()
    {
        var error = Error.NotFound("Order {OrderId} of {Customer} was not found.")
            .WithDetail("OrderId", 7)
            .WithDetail("Customer", "anna@example.com", isSensitive: true)
            .WithCode("ORDER_NOT_FOUND")
            .WithContext("Checkout");

        Assert.Multiple(
            () => Assert.Equal("Checkout: Order 7 of anna@example.com was not found.", error.ToString()),
            () => Assert.Equal("Checkout: Order 7 of *** was not found.", error.ToRedactedString()),
            () => Assert.Equal("Order {OrderId} of {Customer} was not found.", error.Template),
            () => Assert.Equal(ErrorKind.NotFound, error.Kind));
    }

    [Fact]
    internal void The_combine_example_reports_every_failure()
    {
        Result[] checks = [Result.Success(), Result.Failure("Name is missing."), Result.Failure("Email is invalid.")];

        var combined = Result.Combine(checks);

        Assert.Equal("Name is missing., Email is invalid.", combined.GetError().Message);
    }

    [Fact]
    internal void The_partition_example_keeps_the_successes()
    {
        IEnumerable<Result<int>> results = [Result.Success(1), Result.Failure<int>("bad"), Result.Success(3)];

        var (values, errors) = results.Partition();

        Assert.Multiple(
            () => Assert.Equal([1, 3], values),
            () => Assert.Single(errors));
    }

    [Fact]
    internal async Task The_parallel_example_processes_all_items_in_order()
    {
        var result = await new[] { "a", "bb", "ccc" }.SelectInParallel(
            async (text, token) =>
            {
                await Task.Delay(1, token);

                return Result.Success(text.Length);
            },
            maxDegreeOfParallelism: 2,
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal([1, 2, 3], result.GetValue());
    }

    [Fact]
    internal void The_try_example_turns_exceptions_into_results()
    {
        var result = Result.Try(() => int.Parse("not a number", CultureInfo.InvariantCulture));

        Assert.Multiple(
            () => Assert.True(result.IsFailure),
            () => Assert.IsType<FormatException>(result.GetError().Exception));
    }

    // docs/result/getting-started.md
    [Fact]
    internal async Task The_typed_try_example_catches_one_kind_of_exception_and_narrows_it_with_a_condition()
    {
        using var source = new CancellationTokenSource();
        var cancellationToken = source.Token;

        var order = Result.Try(
            () => System.Text.Json.JsonSerializer.Deserialize<Order>("not json"),
            (System.Text.Json.JsonException exception) => Error.FromException(exception, "The order is not valid JSON."));

        var response = await Result.Try(
            static async () =>
            {
                await Task.Yield();

                throw new OperationCanceledException();
            },
            (OperationCanceledException exception) => Error.Timeout("The request timed out.").WithException(exception),
            when: _ => !cancellationToken.IsCancellationRequested);

        Assert.Multiple(
            () => Assert.Equal("The order is not valid JSON.", order.GetError().Message),
            () => Assert.IsType<System.Text.Json.JsonException>(order.GetError().Exception),
            () => Assert.Equal(ErrorKind.Timeout, response.GetError().Kind));
    }

    [Fact]
    internal void The_linq_example_chains_independent_lookups()
    {
        var total = from price in Result.Success(40)
                    from tax in Result.Success(2)
                    select price + tax;

        Assert.Equal(42, total.GetValue());
    }

    [Fact]
    internal void The_safety_example_counts_a_default_result_as_a_success()
    {
        Result unfilled = default;

        Assert.Multiple(
            () => Assert.Equal("Success", unfilled.ToString()),
            () => Assert.True(unfilled.IsSuccess));
    }

    [Fact]
    internal void The_zip_and_flatten_examples_combine_and_unnest_results()
    {
        var triple = Result.Success(1).Zip(Result.Success("a"), Result.Success(true));
        var flat = Result.Success(Result.Success(5)).Flatten();
        var failed = Result.Failure<int>("x").Zip(Result.Failure<string>("y"));

        Assert.Multiple(
            () => Assert.Equal((1, "a", true), triple.GetValue()),
            () => Assert.Equal(5, flat.GetValue()),
            () => Assert.Equal("x, y", failed.GetError().Message));
    }

    [Fact]
    internal void The_hand_written_result_sketch_counts_a_default_as_a_success()
    {
        var unfilled = default(SimpleResult<int>);
        var failed = SimpleResult<int>.Fail("no");

        Assert.Multiple(
            () => Assert.True(unfilled.IsSuccess),
            () => Assert.Equal(6, SimpleResult<int>.Ok(5).Map(static value => value + 1).Match(static value => value, static _ => 0)),
            () => Assert.Equal(-1, failed.Bind(static value => SimpleResult<int>.Ok(value)).Match(static value => value, static _ => -1)));
    }

    private static Result<int> ParseAge(string text) =>
        int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var age)
            ? Result.Success(age)
            : Result.Failure<int>(Error.Validation("'{Text}' is not a number.").WithDetail("Text", text));

    private static Task<Result<Order>> FindOrderAsync(int id) => Task.FromResult(Result.Success(new Order(id, IsOpen: id != 8)));

    private static ValueTask<Result<Receipt>> ChargeAsync(Order order) => new(Result.Success(new Receipt("R-" + order.Id)));

    private readonly record struct SimpleResult<T>(T? Value, string? Error)
    {
        public bool IsSuccess => Error is null;

        public static SimpleResult<T> Ok(T value) => new(value, null);

        public static SimpleResult<T> Fail(string error) => new(default, error);

        public SimpleResult<TNext> Map<TNext>(Func<T, TNext> map) =>
            IsSuccess ? SimpleResult<TNext>.Ok(map(Value!)) : SimpleResult<TNext>.Fail(Error!);

        public SimpleResult<TNext> Bind<TNext>(Func<T, SimpleResult<TNext>> bind) =>
            IsSuccess ? bind(Value!) : SimpleResult<TNext>.Fail(Error!);

        public TOut Match<TOut>(Func<T, TOut> ok, Func<string, TOut> fail) => IsSuccess ? ok(Value!) : fail(Error!);
    }

    private sealed record Order(int Id, bool IsOpen);

    private sealed record Receipt(string Number);
}
