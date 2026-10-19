// A tiny in-memory shop that every example of this project uses, so that the examples can talk about
// orders, customers, stock and payments instead of inventing a new domain each time.
//
// The shop is written the way real code written with this library looks: an operation that can fail in an
// EXPECTED way (the order does not exist, the stock is too low, the customer is blocked) returns a Result.
// Nothing here throws for those cases. The data is fixed, and nothing is ever changed, so the examples can run
// in any order, and at the same time.
namespace Wyzely.Functional.ResultSamples;

public sealed record Customer(int Id, string Name, string Email, bool IsBlocked);

public sealed record OrderLine(string Sku, int Quantity);

public sealed record Order(int Id, int CustomerId, IReadOnlyList<OrderLine> Lines, bool IsOpen);

public sealed record Receipt(string Number, int AmountInCents);

public static class SampleShop
{
    private static readonly Dictionary<int, Customer> Customers = new()
    {
        [1] = new Customer(1, "Anna", "anna@example.com", IsBlocked: false),
        [2] = new Customer(2, "Bo", "bo@example.com", IsBlocked: true),
    };

    private static readonly Dictionary<int, Order> Orders = new()
    {
        [7] = new Order(7, 1, [new OrderLine("TEA", 2)], IsOpen: true),
        [8] = new Order(8, 1, [new OrderLine("TEA", 1)], IsOpen: false),
        [9] = new Order(9, 2, [new OrderLine("TEA", 1)], IsOpen: true),
        [11] = new Order(11, 1, [new OrderLine("COFFEE", 3)], IsOpen: true),
        [12] = new Order(12, 1, [new OrderLine("TEA", 1)], IsOpen: true),
    };

    private static readonly Dictionary<string, int> Stock = new() { ["TEA"] = 5, ["COFFEE"] = 1 };

    private static readonly Dictionary<string, int> PricesInCents = new() { ["TEA"] = 250, ["COFFEE"] = 400 };

    // "Not found" is an ordinary outcome, not an exception. The message is a TEMPLATE: the placeholders are filled
    // from the details, and the template itself stays stable, which is what you group log entries by.
    public static Result<Customer> FindCustomer(int id) =>
        Customers.TryGetValue(id, out var customer)
            ? Result.Success(customer)
            : Result.Failure<Customer>(Error.NotFound("Customer {CustomerId} was not found.").WithDetail("CustomerId", id));

    public static Result<Order> FindOrder(int id) =>
        Orders.TryGetValue(id, out var order)
            ? Result.Success(order)
            : Result.Failure<Order>(Error.NotFound("Order {OrderId} was not found.").WithDetail("OrderId", id));

    // An operation that produces nothing but "it worked" or "it did not" returns the untyped Result.
    public static Result ReserveStock(Order order)
    {
        foreach (var line in order.Lines)
        {
            var have = Stock.GetValueOrDefault(line.Sku);

            if (have < line.Quantity)
            {
                return Result.Failure(
                    Error.Conflict("Not enough {Sku} in stock (wanted {Wanted}, have {Have}).")
                        .WithDetail("Sku", line.Sku)
                        .WithDetail("Wanted", line.Quantity)
                        .WithDetail("Have", have));
            }
        }

        return Result.Success();
    }

    // Charging a card: a blocked customer is refused (Forbidden), and order 12 meets a payment gateway that is
    // down (Unavailable, which is marked transient: trying again later might work).
    public static Result<Receipt> Charge(Order order) =>
        FindCustomer(order.CustomerId)
            .Ensure(
                customer => !customer.IsBlocked,
                Error.Forbidden("Customer {CustomerId} is blocked.").WithDetail("CustomerId", order.CustomerId))
            .Ensure(_ => order.Id != 12, Error.Unavailable("The payment gateway is unavailable."))
            .Map(_ => new Receipt("R-" + order.Id, PriceInCents(order)));

    // The fallback payment method: it always works in this shop.
    public static Result<Receipt> Invoice(Order order) => Result.Success(new Receipt("I-" + order.Id, PriceInCents(order)));

    public static async Task<Result<Order>> FindOrderAsync(int id, CancellationToken cancellationToken = default)
    {
        await Task.Yield();
        cancellationToken.ThrowIfCancellationRequested();

        return FindOrder(id);
    }

    public static async Task<Result> ReserveStockAsync(Order order)
    {
        await Task.Yield();

        return ReserveStock(order);
    }

    public static async ValueTask<Result<Receipt>> ChargeAsync(Order order)
    {
        await Task.Yield();

        return Charge(order);
    }

    private static int PriceInCents(Order order)
    {
        var total = 0;

        foreach (var line in order.Lines)
        {
            total += PricesInCents[line.Sku] * line.Quantity;
        }

        return total;
    }
}
