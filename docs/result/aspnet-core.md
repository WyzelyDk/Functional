# Mapping errors in ASP.NET Core

[Back to the Result overview](../../src/Result/README.md)

An expected failure is a value, and at the edge of a web application it has to become a response. This page shows how to turn an `Error` into a problem
response (`application/problem+json`, RFC 9457) and a `Result<T>` into a response for a minimal API.

The library has no dependency on ASP.NET Core and does not map errors to HTTP: which status a kind of error gets is a policy of the application
([scope](scope.md)). So this is a recipe to copy and change, not a feature. The code is in [`samples/AspNetCore.Samples`](../../samples/AspNetCore.Samples),
and `test/Samples.Tests` runs it with a real request to an app on a loopback port, and checks that this page shows the same code, so it cannot drift.

## The mapping

The code of `ProblemMapping.cs`:

```csharp
using Microsoft.AspNetCore.Mvc;

namespace Wyzely.Functional.AspNetCoreSamples;

public static class ProblemMapping
{
    // The status code of each kind of error. A kind that is not listed (Failure, Unexpected, or a kind of your own) is a 500.
    private static readonly Dictionary<ErrorKind, int> StatusOfKind = new()
    {
        [ErrorKind.Validation] = StatusCodes.Status400BadRequest,
        [ErrorKind.Unauthorized] = StatusCodes.Status401Unauthorized,
        [ErrorKind.Forbidden] = StatusCodes.Status403Forbidden,
        [ErrorKind.NotFound] = StatusCodes.Status404NotFound,
        [ErrorKind.Conflict] = StatusCodes.Status409Conflict,
        [ErrorKind.Unavailable] = StatusCodes.Status503ServiceUnavailable,
        [ErrorKind.Timeout] = StatusCodes.Status504GatewayTimeout,
    };

    public static ProblemDetails ToProblemDetails(this Error error)
    {
        var status = StatusOfKind.GetValueOrDefault(error.Kind, StatusCodes.Status500InternalServerError);

        // RedactedMessage, not Message: a detail that is marked sensitive must not reach the client.
        var problem = new ProblemDetails { Status = status, Title = error.Kind.Name, Detail = error.RedactedMessage };

        if (error.Code is not null)
        {
            problem.Extensions["code"] = error.Code;
        }

        var details = error.Details
            .Where(static detail => !detail.IsSensitive)
            .ToDictionary(static detail => detail.Name, static detail => detail.Value);

        if (details.Count > 0)
        {
            problem.Extensions["details"] = details;
        }

        if (error.IsAggregate)
        {
            problem.Extensions["errors"] = error.Children.Select(static child => child.RedactedMessage).ToList();
        }

        return problem;
    }

    public static IResult ToHttpResult(this Error error)
    {
        var problem = error.ToProblemDetails();

        return Results.Problem(problem);
    }

    public static IResult ToHttpResult<T>(this Result<T> result) =>
        result.Match(static value => Results.Ok(value), static error => error.ToHttpResult());
}
```

- **The status comes from the kind of the error.** The dictionary is the policy: `Validation` is a 400, `Unavailable` a 503, `Timeout` a 504, and so on.
  Those are the choices of the sample, so change them. `ErrorKind` is open (`new ErrorKind("RateLimited")` is a kind of your own), and a kind that is not
  listed is a 500 until you add it.
- **The detail is `RedactedMessage`, never `Message`.** A detail that is marked sensitive is replaced by `***` in it, so it does not reach the client
  ([errors](errors.md)).
- **The extensions** carry the code of the error (`code`), the details that are not sensitive (`details`), and for an aggregate error, such as the
  result of `Combine`, the messages of its children (`errors`).
- **`ToHttpResult` turns a `Result<T>` into a response:** a value is a 200 with the value, and an error is the problem response.

## An endpoint

The code of `OrderEndpoints.cs`, where the service returns a `Task<Result<Order>>`:

```csharp
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
```

`app.MapOrders()` registers it. The handler is a method, not a lambda, so that it reads top to bottom and a debugger can step through it.

## What the client gets

`dotnet run --project samples/AspNetCore.Samples` starts the app, makes two requests (`OrdersExample`) and prints the responses:

```text
GET /orders/1: 200 application/json
{
  "id": 1,
  "customer": "Ada"
}
GET /orders/2: 404 application/problem+json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.5",
  "title": "NotFound",
  "status": 404,
  "detail": "Customer *** has no order 2.",
  "details": {
    "OrderId": 2
  }
}
```

The error of the second request carries the e-mail address of a customer as a sensitive detail. It is in the message that the server can log, and it is not
in the response: the detail is `Customer *** has no order 2.`, and `details` has only the order number. The `type` is the default that ASP.NET Core gives
a problem response for the status.

## What to decide in your own code

- **Messages of a 5xx.** An error made with `Error.FromException` uses the message of the exception when you give none, and that text is meant for a log.
  For a 500 you may prefer a fixed detail to `RedactedMessage`; it is one line in `ToProblemDetails`.
- **A canceled request.** `ErrorKind.Canceled` is not mapped: when the request was canceled, the client has gone, and there is nobody to answer.
- **Controllers.** `ToProblemDetails` returns the `ProblemDetails` of ASP.NET Core, so a controller can use it to build its response. The sample does not
  cover controllers.
- **Logging.** Log the error on the server with its full `Message`; the library does not log ([scope](scope.md)).
