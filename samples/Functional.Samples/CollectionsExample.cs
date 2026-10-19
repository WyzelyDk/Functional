using System.Globalization;

namespace Wyzely.Functional.FunctionalSamples;

// SCENARIO: orders and a price list. We look things up in lists and dictionaries (which may find nothing) and then have to decide what
// a missing thing means for a whole batch.
//
// WHY: the Maybe helpers (FirstOrNone, GetValueOrNone, Choose) replace "FirstOrDefault and then a null check". Then the two types meet:
// when each element becomes a Result, Combine reports every failure in one aggregate error, and Partition keeps the good ones and the
// bad ones apart. Zip does the same for a few independent values of different types.
public static class CollectionsExample
{
    private static readonly string[] Emails = ["ada@example.com", "nobody@example.com", "grace@example.com"];

    private static readonly OrderLine[] GoodLines = [new("APPLE", 2), new("MILK", 3)];

    private static readonly OrderLine[] BadLines = [new("APPLE", 1), new("CAKE", 1), new("TEA", 2)];

    public static void Run(TextWriter output)
    {
        var directory = new CustomerDirectory();
        var prices = new Dictionary<string, int> { ["APPLE"] = 3, ["BREAD"] = 5, ["MILK"] = 2 };

        OrderSummary[] orders =
        [
            new(1, "ada@example.com", 40, IsPaid: true),
            new(2, "grace@example.com", 120, IsPaid: false),
            new(3, "ada@example.com", 300, IsPaid: false),
        ];

        // 1. FirstOrNone: the first element, or none, instead of FirstOrDefault and a null check. It composes with the Maybe railway.
        var firstUnpaid = orders.FirstOrNone(order => !order.IsPaid);
        output.WriteLine($"FirstOrNone, first unpaid order: {firstUnpaid.Match(order => order.Id.ToString(CultureInfo.InvariantCulture), () => "none")}");

        var customerOfFirstUnpaid = firstUnpaid
            .Map(order => order.CustomerEmail)
            .Bind(directory.FindByEmail);
        output.WriteLine($"Customer of the first unpaid order: {customerOfFirstUnpaid.Match(customer => customer.Name, () => "none")}");
        var expensive = orders.FirstOrNone(order => order.Total > 1000).Match(order => order.Id.ToString(CultureInfo.InvariantCulture), () => "none");
        output.WriteLine($"FirstOrNone, order above 1000: {expensive}");

        // When a step already returns a Maybe, Map would give a Maybe<Maybe<T>>. Flatten makes it one Maybe (Bind does the same in one
        // step). Result<Result<T>> has a Flatten too.
        var nested = firstUnpaid.Map(order => directory.FindByEmail(order.CustomerEmail));
        output.WriteLine($"Flatten, Maybe<Maybe<Customer>>: {nested.Flatten().Match(customer => customer.Name, () => "none")}");

        // 2. Choose: map every element to a Maybe and keep only the ones that have a value.
        var known = Emails
            .Choose(directory.FindByEmail)
            .Select(customer => customer.Name);
        output.WriteLine($"Choose, customers that exist: {string.Join(", ", known)}");

        // 3. GetValueOrNone: a dictionary lookup as a Maybe. It exists for IReadOnlyDictionary only (a Dictionary is one).
        var milk = prices.GetValueOrNone("MILK").Match(price => price.ToString(CultureInfo.InvariantCulture), () => "unknown");
        var cake = prices.GetValueOrNone("CAKE").Match(price => price.ToString(CultureInfo.InvariantCulture), () => "unknown");
        output.WriteLine($"GetValueOrNone, price of MILK: {milk}");
        output.WriteLine($"GetValueOrNone, price of CAKE: {cake}");

        // 4. Maybe becomes Result for every element, then Combine. A line whose SKU is not in the price list is a NotFound failure that
        //    names the SKU. The factory (Func<Error>) is used so that an error is only built for the lines that need one.
        Result<int> PriceOf(OrderLine line) => prices.GetValueOrNone(line.Sku)
            .ToResult(() => Error.NotFound("Unknown SKU {Sku}.").WithDetail("Sku", line.Sku))
            .Map(price => price * line.Quantity);

        var combined = GoodLines.Select(PriceOf).Combine();
        var combinedText = combined.Match(totals => $"{string.Join(", ", totals)} (total {totals.Sum()})", error => error.Message);
        output.WriteLine($"Combine, all lines priced: {combinedText}");

        // Combine does not stop at the first failure: the error is an aggregate of all of them, in the order of the lines. Its kind is
        // the kind the children share (here NotFound), or the plain Failure kind when they differ.
        var badResults = BadLines.Select(PriceOf).ToList();
        var badCombined = badResults.Combine();

        if (badCombined.TryGetError(out var aggregate))
        {
            output.WriteLine($"Combine, some lines unknown: {aggregate.Children.Count} problems of kind {aggregate.Kind}");

            foreach (var child in aggregate.Flatten())
            {
                output.WriteLine($"  - {child.Message}");
            }
        }

        // 5. Partition: keep what worked and what did not, side by side, when a partial answer is useful.
        var (priced, rejected) = badResults.Partition();
        output.WriteLine($"Partition: {priced.Count} priced ({string.Join(", ", priced)}), {rejected.Count} rejected");

        // 6. Zip: a few independent values of different types, in one step. For Maybe the result is none unless all have a value.
        var buyer = directory.FindByEmail("grace@example.com");
        var milkOrder = buyer.Zip(prices.GetValueOrNone("MILK"), (customer, price) => $"{customer.Name} buys MILK at {price}").ValueOr("none");
        var cakeOrder = buyer.Zip(prices.GetValueOrNone("CAKE"), (customer, price) => $"{customer.Name} buys CAKE at {price}").ValueOr("none");
        output.WriteLine($"Maybe.Zip, both present: {milkOrder}");
        output.WriteLine($"Maybe.Zip, one missing: {cakeOrder}");

        // For Result, Zip reports every failure, not just the first: here a missing customer AND an unknown SKU, in argument order.
        var missingBuyer = directory.FindByEmail("nobody@example.com").ToResult(Error.NotFound("No customer with that email address."));
        var unknownPrice = PriceOf(new OrderLine("CAKE", 1));
        var zipped = missingBuyer.Zip(unknownPrice, (customer, total) => $"{customer.Name} owes {total}");
        var messages = zipped.Match(_ => "success", error => string.Join(" | ", error.Flatten().Select(child => child.Message)));
        output.WriteLine($"Result.Zip, both failed, in argument order: {messages}");
    }

    private sealed record OrderSummary(int Id, string CustomerEmail, int Total, bool IsPaid);

    private sealed record OrderLine(string Sku, int Quantity);
}
