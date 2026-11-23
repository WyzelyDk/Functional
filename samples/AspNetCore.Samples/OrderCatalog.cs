namespace Wyzely.Functional.AspNetCoreSamples;

// The service of the example: an order that exists, and a failure for every other one. The failure carries the e-mail address of a customer as a
// sensitive detail, which the response must not contain.
public static class OrderCatalog
{
    public static async Task<Result<Order>> FindAsync(int id)
    {
        await Task.Yield();

        if (id == 1)
        {
            var order = new Order(1, "Ada");

            return Result.Success(order);
        }

        var missing = Error.NotFound("Customer {Email} has no order {OrderId}.")
            .WithDetail("Email", "anna@example.com", isSensitive: true)
            .WithDetail("OrderId", id);

        return Result.Failure<Order>(missing);
    }
}
