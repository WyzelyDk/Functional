namespace Wyzely.Functional.AspNetCoreSamples;

public static class OrderEndpoints
{
    public static void MapOrders(this IEndpointRouteBuilder routes) => routes.MapGet("/orders/{id:int}", GetOrder);

    private static async Task<IResult> GetOrder(int id)
    {
        var found = await OrderCatalog.FindAsync(id);

        return found.ToHttpResult();
    }
}
