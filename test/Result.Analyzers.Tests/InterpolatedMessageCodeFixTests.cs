using Wyzely.Functional.ResultAnalyzers;

namespace Wyzely.Functional.ResultAnalyzersTests;

// The fix moves each value out of the text and into a detail with the name of the placeholder, so the message stays the same for the reader.
public sealed class InterpolatedMessageCodeFixTests
{
    private const string Sources = """
        record Order(int Id, string Customer);
        """;

    private static readonly InterpolatedMessageCodeFixProvider Provider = new();
    private static readonly ResultMessageAnalyzer Analyzer = new();

    [Theory]
    [InlineData(
        "Error M(int id) => Error.Create($\"Order {id} not found.\");",
        "Error.Create(\"Order {id} not found.\").WithDetail(\"id\", id)")]
    [InlineData(
        "Error M(int id) => Error.NotFound($\"Order {id} not found.\");",
        "Error.NotFound(\"Order {id} not found.\").WithDetail(\"id\", id)")]
    [InlineData(
        "Error M(Order order) => Error.Create($\"Order {order.Id} of {order.Customer} not found.\");",
        "Error.Create(\"Order {Id} of {Customer} not found.\").WithDetail(\"Id\", order.Id).WithDetail(\"Customer\", order.Customer)")]
    [InlineData(
        "Error M(int id) => Error.Create($\"Order {id} not found.\", ErrorKind.NotFound);",
        "Error.Create(\"Order {id} not found.\", ErrorKind.NotFound).WithDetail(\"id\", id)")]
    [InlineData(
        "Error M(int id) => Error.Create($\"Order {id} costs {{5}} and \\\"more\\\".\");",
        "Error.Create(\"Order {id} costs {{5}} and \\\"more\\\".\").WithDetail(\"id\", id)")]
    [InlineData(
        "Result M(int id) => Result.Failure($\"Order {id} not found.\");",
        "Result.Failure(Error.Create(\"Order {id} not found.\").WithDetail(\"id\", id))")]
    [InlineData(
        "Result<int> M(int id) => Result.Failure<int>($\"Order {id} not found.\");",
        "Result.Failure<int>(Error.Create(\"Order {id} not found.\").WithDetail(\"id\", id))")]
    [InlineData(
        "Error M(Error e, int id) => e.WithContext($\"Checkout of {id}\");",
        "e.WithContext(\"Checkout of {id}\", \"id\", id)")]
    [InlineData(
        "Error M(Error e, Order order) => e.WithContext($\"Checkout of {order.Id} for {order.Customer}\");",
        "e.WithContext(\"Checkout of {Id} for {Customer}\", new ErrorDetail(\"Id\", order.Id), new ErrorDetail(\"Customer\", order.Customer))")]
    internal async Task The_fix_moves_the_values_into_details_and_the_warning_is_gone(string method, string expected)
    {
        var outcome = await CodeFixTestHost.FixAsync(Sources + "\n" + method, Provider, "WYZ0111", Analyzer);

        Assert.Multiple(
            () => Assert.NotNull(outcome.FixedSource),
            () => Assert.Contains(expected, outcome.FixedSource, StringComparison.Ordinal),
            () => Assert.DoesNotContain("WYZ0111", outcome.RemainingIds));
    }

    [Theory]
    [InlineData("Error M(double x) => Error.Create($\"Value {x:N2} is too big.\");")]
    [InlineData("Error M(int a, int b) => Error.Create($\"Sum {a + b} is too big.\");")]
    [InlineData("Error M(int id) => Error.Create($\"Order {id} is not {id}.\");")]
    [InlineData("Error M(Order a, Order b) => Error.Create($\"Orders {a.Id} and {b.Id} differ.\");")]
    [InlineData("Error M(int id) => Error.Create($@\"Order {id} not found.\");")]
    [InlineData("Result<int> M(Result<int> r, int id) => r.Ensure(x => x > 0, $\"Value {id} is not positive.\");")]
    internal async Task No_fix_is_offered_when_the_values_cannot_be_named_without_changing_the_message(string method)
    {
        var outcome = await CodeFixTestHost.FixAsync(Sources + "\n" + method, Provider, "WYZ0111", Analyzer);

        Assert.Null(outcome.FixedSource);
    }

    [Fact]
    internal void The_provider_fixes_the_rule_and_supports_fixing_all_occurrences()
    {
        Assert.Multiple(
            () => Assert.Equal(["WYZ0111"], Provider.FixableDiagnosticIds),
            () => Assert.NotNull(Provider.GetFixAllProvider()));
    }
}
