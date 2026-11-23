using System.Text.Json;
using Microsoft.AspNetCore.Http.HttpResults;
using Wyzely.Functional.AspNetCoreSamples;

namespace Wyzely.Functional.SamplesTests;

// The ASP.NET Core recipe (docs/result/aspnet-core.md) is the code of samples/AspNetCore.Samples. The mapping is tested on its own, then through a
// real request to an app on a loopback port, and the page is checked against the code that it shows.
public sealed class AspNetCoreSamplesTests
{
    [Theory]
    [InlineData("Validation", 400)]
    [InlineData("Unauthorized", 401)]
    [InlineData("Forbidden", 403)]
    [InlineData("NotFound", 404)]
    [InlineData("Conflict", 409)]
    [InlineData("Unavailable", 503)]
    [InlineData("Timeout", 504)]
    [InlineData("Unexpected", 500)]
    [InlineData("Failure", 500)]
    [InlineData("RateLimited", 500)]
    internal void ToProblemDetails_maps_the_kind_of_an_error_to_its_status_code(string kind, int status)
    {
        var error = Error.Create("Nothing to see.", new ErrorKind(kind));

        var problem = error.ToProblemDetails();

        Assert.Multiple(
            () => Assert.Equal(status, problem.Status),
            () => Assert.Equal(kind, problem.Title));
    }

    [Fact]
    internal void ToProblemDetails_uses_the_redacted_message_so_that_a_sensitive_detail_does_not_reach_the_client()
    {
        var error = Error.NotFound("Customer {Email} has no order {OrderId}.")
            .WithDetail("Email", "anna@example.com", isSensitive: true)
            .WithDetail("OrderId", 7);

        var problem = error.ToProblemDetails();
        var json = JsonSerializer.Serialize(problem);

        Assert.Multiple(
            () => Assert.Equal(error.RedactedMessage, problem.Detail),
            () => Assert.DoesNotContain("anna@example.com", json));
    }

    [Fact]
    internal void ToProblemDetails_puts_the_code_and_the_details_that_are_not_sensitive_in_the_extensions()
    {
        var error = Error.Conflict("Order {OrderId} was changed by {Email}.")
            .WithCode("ORDER_CHANGED")
            .WithDetail("OrderId", 7)
            .WithDetail("Email", "anna@example.com", isSensitive: true);

        var problem = error.ToProblemDetails();

        var details = Assert.IsType<IDictionary<string, object?>>(problem.Extensions["details"], exactMatch: false);
        Assert.Multiple(
            () => Assert.Equal("ORDER_CHANGED", problem.Extensions["code"]),
            () => Assert.Equal(7, details["OrderId"]),
            () => Assert.False(details.ContainsKey("Email")));
    }

    [Fact]
    internal void ToProblemDetails_lists_the_children_of_an_aggregate_error()
    {
        var error = Error.Aggregate([Error.Validation("Name is required."), Error.Validation("Quantity must be positive.")]);

        var problem = error.ToProblemDetails();

        var errors = Assert.IsType<IEnumerable<string>>(problem.Extensions["errors"], exactMatch: false);
        Assert.Equal(["Name is required.", "Quantity must be positive."], errors);
    }

    [Fact]
    internal void ToHttpResult_gives_a_problem_response_for_a_failure_and_a_value_for_a_success()
    {
        var failed = Result.Failure<int>(Error.NotFound("No such order.")).ToHttpResult();
        var succeeded = Result.Success(42).ToHttpResult();

        Assert.Multiple(
            () => Assert.Equal(404, Assert.IsType<ProblemHttpResult>(failed).StatusCode),
            () => Assert.Equal(42, Assert.IsType<Ok<int>>(succeeded).Value));
    }

    [Fact]
    internal async Task OrdersExample_answers_a_real_request_with_the_value_and_with_a_problem_that_hides_the_sensitive_detail()
    {
        using var writer = new StringWriter();

        await OrdersExample.Run(writer);

        var lines = writer.ToString().Split(["\r\n", "\n"], StringSplitOptions.RemoveEmptyEntries);
        Assert.Multiple(
            () => Assert.Contains("GET /orders/1: 200 application/json", lines),
            () => Assert.Contains("  \"customer\": \"Ada\"", lines),
            () => Assert.Contains("GET /orders/2: 404 application/problem+json", lines),
            () => Assert.Contains("  \"status\": 404,", lines),
            () => Assert.Contains("  \"detail\": \"Customer *** has no order 2.\",", lines),
            () => Assert.DoesNotContain(lines, static line => line.Contains("anna@example.com", StringComparison.Ordinal)));
    }

    [Fact]
    internal void The_page_for_ASP_NET_Core_shows_the_code_of_the_sample()
    {
        var root = RepositoryRoot();
        var page = ReadText(root, "docs", "result", "aspnet-core.md");

        var shown = CodeBlocks(page, "csharp");

        Assert.NotEmpty(shown);
        Assert.All(shown, block => Assert.Contains(block.Code, ReadText(root, "samples", "AspNetCore.Samples", block.File), StringComparison.Ordinal));
    }

    [Fact]
    internal async Task The_page_for_ASP_NET_Core_shows_what_the_example_prints()
    {
        var page = ReadText(RepositoryRoot(), "docs", "result", "aspnet-core.md");
        using var writer = new StringWriter();

        await OrdersExample.Run(writer);

        var printed = writer.ToString().Replace("\r\n", "\n", StringComparison.Ordinal).TrimEnd('\n');
        var shown = Assert.Single(CodeBlocks(page, "text"));
        Assert.Equal(printed, shown.Code);
    }

    private static string ReadText(params string[] path) => File.ReadAllText(Path.Combine(path)).Replace("\r\n", "\n", StringComparison.Ordinal);

    // The fenced blocks of a language, each with the file that the line before it names in backticks (`ProblemMapping.cs`) or, for the printed output,
    // the file of the example that prints it.
    private static List<(string File, string Code)> CodeBlocks(string page, string language)
    {
        var blocks = new List<(string, string)>();
        var lines = page.Split('\n');

        for (var index = 1; index < lines.Length; index++)
        {
            if (lines[index] != "```" + language)
            {
                continue;
            }

            var end = Array.FindIndex(lines, index + 1, static line => line == "```");
            var file = lines[index - 2].Split('`').FirstOrDefault(static part => part.EndsWith(".cs", StringComparison.Ordinal)) ?? string.Empty;
            blocks.Add((file, string.Join('\n', lines[(index + 1)..end])));
        }

        return blocks;
    }

    private static string RepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Wyzely.Functional.slnx")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new InvalidOperationException("The repository root was not found.");
    }
}
