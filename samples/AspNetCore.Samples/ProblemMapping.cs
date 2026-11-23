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
