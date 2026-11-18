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

    private static readonly Dictionary<int, Order> Orders = CreateOrders();

    private static readonly Dictionary<string, int> Stock = new() { ["TEA"] = 5, ["COFFEE"] = 1 };

    private static readonly Dictionary<string, int> PricesInCents = new() { ["TEA"] = 250, ["COFFEE"] = 400 };

    // "Not found" is an ordinary outcome, not an exception. The message is a TEMPLATE: the placeholders are filled from the details,
    // and the template itself stays stable, which is what you group log entries by.
    public static Result<Customer> FindCustomer(int id)
    {
        if (Customers.TryGetValue(id, out var customer))
        {
            return Result.Success(customer);
        }

        var notFound = Error.NotFound("Customer {CustomerId} was not found.").WithDetail("CustomerId", id);

        return Result.Failure<Customer>(notFound);
    }

    public static Result<Order> FindOrder(int id)
    {
        if (Orders.TryGetValue(id, out var order))
        {
            return Result.Success(order);
        }

        var notFound = Error.NotFound("Order {OrderId} was not found.").WithDetail("OrderId", id);

        return Result.Failure<Order>(notFound);
    }

    // An operation that produces nothing but "it worked" or "it did not" returns the untyped Result.
    public static Result ReserveStock(Order order)
    {
        foreach (var line in order.Lines)
        {
            var have = Stock.GetValueOrDefault(line.Sku);

            if (have < line.Quantity)
            {
                var shortage = Error.Conflict("Not enough {Sku} in stock (wanted {Wanted}, have {Have}).")
                    .WithDetail("Sku", line.Sku)
                    .WithDetail("Wanted", line.Quantity)
                    .WithDetail("Have", have);

                return Result.Failure(shortage);
            }
        }

        return Result.Success();
    }

    // Charging a card: a blocked customer is refused (Forbidden), and order 12 meets a payment gateway that is down (Unavailable, which is marked transient:
    // trying again later might work).
    public static Result<Receipt> Charge(Order order)
    {
        var found = FindCustomer(order.CustomerId);
        var blocked = Error.Forbidden("Customer {CustomerId} is blocked.").WithDetail("CustomerId", order.CustomerId);
        var unavailable = Error.Unavailable("The payment gateway is unavailable.");

        return found
            .Ensure(customer => !customer.IsBlocked, blocked)
            .Ensure(_ => order.Id != 12, unavailable)
            .Map(_ =>
            {
                var amount = PriceInCents(order);

                return new Receipt("R-" + order.Id, amount);
            });
    }

    // The fallback payment method: it always works in this shop.
    public static Result<Receipt> Invoice(Order order)
    {
        var amount = PriceInCents(order);
        var receipt = new Receipt("I-" + order.Id, amount);

        return Result.Success(receipt);
    }

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

    private static Dictionary<int, Order> CreateOrders()
    {
        OrderLine[] twoTea = [new OrderLine("TEA", 2)];
        OrderLine[] oneTea = [new OrderLine("TEA", 1)];
        OrderLine[] threeCoffee = [new OrderLine("COFFEE", 3)];

        return new Dictionary<int, Order>
        {
            [7] = new Order(7, 1, twoTea, IsOpen: true),
            [8] = new Order(8, 1, oneTea, IsOpen: false),
            [9] = new Order(9, 2, oneTea, IsOpen: true),
            [11] = new Order(11, 1, threeCoffee, IsOpen: true),
            [12] = new Order(12, 1, oneTea, IsOpen: true),
        };
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
