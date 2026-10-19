using System.Globalization;

namespace Wyzely.Functional.FunctionalSamples;

// SCENARIO: order checkout. Everything in this file is self-contained (its own small types and in-memory data), so you can copy it.
//
// The flow: look the customer up (Maybe) and check the order lines (a Maybe lookup per line) INDEPENDENTLY, so a customer who is
// unknown AND orders an unknown product hears about both. Then charge (async, may fail), add context to any failure, log it, and turn
// the outcome into a response. Maybe and Result meet at ToResult.
public static class CheckoutExample
{
    private const string Apple = "APPLE";

    private static readonly Dictionary<string, int> Prices = new() { [Apple] = 3, ["MILK"] = 2 };

    private static readonly Dictionary<string, Customer> Customers = new[]
    {
        new Customer("ada@example.com", IsActive: true),
        new Customer("linus@example.com", IsActive: false),
        new Customer("alan@example.com", IsActive: true),
    }.ToDictionary(customer => customer.Email);

    private static readonly (string Email, (string Sku, int Quantity)[] Lines)[] Requests =
    [
        ("ada@example.com", [(Apple, 2), ("MILK", 1)]),
        ("nobody@example.com", [(Apple, 1)]),
        ("linus@example.com", [(Apple, 1)]),
        ("ada@example.com", [("CAKE", 1), ("TEA", 1)]),
        ("alan@example.com", [("MILK", 1)]),
        ("nobody@example.com", [("CAKE", 1)]),
    ];

    public static void Run(TextWriter output) => RunAsync(output).GetAwaiter().GetResult();

    private static async Task RunAsync(TextWriter output)
    {
        var failures = new List<string>();

        foreach (var (email, lines) in Requests)
        {
            var response = await CheckoutAsync(email, lines, failures);
            await output.WriteLineAsync($"{email}: {response}");
        }

        await output.WriteLineAsync($"Failures logged: {failures.Count}");
    }

    private static async Task<string> CheckoutAsync(string email, (string Sku, int Quantity)[] lines, List<string> log)
    {
        // Maybe<Customer>, made a Result, with a rule on top.
        var customer = Customers.GetValueOrNone(email)
            .ToResult(Error.NotFound("No customer with that email address."))
            .Ensure(found => found.IsActive, Error.Forbidden("The customer is blocked."));

        // A Maybe<int> per line, made a Result<int>; Combine reports every unknown product, not just the first.
        var total = lines
            .Select(line => Prices.GetValueOrNone(line.Sku)
                .ToResult(() => Error.NotFound("Unknown product {Sku}.").WithDetail("Sku", line.Sku))
                .Map(price => price * line.Quantity))
            .Combine()
            .Map(totals => totals.Sum());

        // Zip reports the failures of BOTH sides when both failed (an aggregate, in argument order). Bind charges through a
        // ValueTask<Result<string>> step, so the chain becomes asynchronous there without an await. MapError is the one place to say
        // where it happened (a context frame), and TapError logs the error without its sensitive details.
        return await customer
            .Zip(total, (who, amount) => (Customer: who, Amount: amount))
            .Bind(order => ChargeAsync(order.Customer, order.Amount))
            .MapError(error => error.WithContext("Checkout"))
            .TapError(error => log.Add(error.ToRedactedString()))
            .Match(
                receipt => $"Paid {receipt}",
                error => $"Rejected [{error.Kind}] {error}{(error.IsTransient ? " (retry later)" : string.Empty)}");
    }

    private static async ValueTask<Result<string>> ChargeAsync(Customer customer, int amount)
    {
        await Task.Yield();

        return customer.Email.StartsWith("alan", StringComparison.Ordinal)
            ? Result.Failure<string>(Error.Unavailable("The payment gateway did not answer."))
            : Result.Success($"R-{customer.Email.Split('@')[0]} for {amount.ToString(CultureInfo.InvariantCulture)}");
    }

    private sealed record Customer(string Email, bool IsActive);
}
