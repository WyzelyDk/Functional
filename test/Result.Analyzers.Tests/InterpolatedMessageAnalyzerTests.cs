using Wyzely.Functional.ResultAnalyzers;

namespace Wyzely.Functional.ResultAnalyzersTests;

// A message with a value interpolated into it is built on every call, also when nothing fails, and it hides the value from the template,
// the details and the redaction of the error.
public sealed class InterpolatedMessageAnalyzerTests
{
    private const string Sources = """
        const string Prefix = "Order";
        record Order(int Id);
        """;

    private static readonly ResultMessageAnalyzer Analyzer = new();

    [Theory]
    [InlineData("Error M(int id) => Error.Create($\"Order {id} not found.\");")]
    [InlineData("Error M(int id) => Error.NotFound($\"Order {id} not found.\");")]
    [InlineData("Error M(int id) => Error.Create($\"Order {id} not found.\", ErrorKind.NotFound);")]
    [InlineData("Result M(int id) => Result.Failure($\"Order {id} not found.\");")]
    [InlineData("Result<int> M(int id) => Result.Failure<int>($\"Order {id} not found.\");")]
    [InlineData("Error M(Error e, int id) => e.WithContext($\"Checkout of {id}\");")]
    [InlineData("Result<int> M(Result<int> r, int id) => r.Ensure(x => x > 0, $\"Value {id} is not positive.\");")]
    [InlineData("Task<Result<int>> M(Task<Result<int>> r, int id) => r.Ensure(x => x > 0, $\"Value {id} is not positive.\");")]
    [InlineData("Error M(Order order) => Error.Create($\"Order {order.Id} not found.\");")]
    [InlineData("Error M(int id) => Error.Create($\"{Prefix} {id} not found.\");")]
    internal async Task A_message_with_a_value_interpolated_into_it_is_reported(string method)
    {
        var ids = await AnalyzerTestHost.AnalyzeAsync(Sources + "\n" + method, Analyzer);

        Assert.Equal(["WYZ0111"], ids);
    }

    [Theory]
    [InlineData("Error M() => Error.Create($\"Order not found.\");")]
    [InlineData("Error M() => Error.Create($\"{Prefix} not found.\");")]
    [InlineData("Error M() => Error.Create(\"Order {id} not found.\");")]
    [InlineData("Error M(string text) => Error.Create(text);")]
    [InlineData("Error M(int id) => Error.Create(string.Format(\"Order {0} not found.\", id));")]
    [InlineData("Error M(Error e) => e.WithContext(\"Checkout\");")]
    [InlineData("Error M(Error e, int id) => e.WithContext(\"Checkout of {Id}\", \"Id\", id);")]
    [InlineData("Error M(Error e, int id) => e.WithCode($\"CODE_{id}\");")]
    [InlineData("void M(int id) => Console.WriteLine($\"Order {id} not found.\");")]
    internal async Task Messages_that_are_constant_or_not_built_for_an_error_are_left_alone(string method)
    {
        var ids = await AnalyzerTestHost.AnalyzeAsync(Sources + "\n" + method, Analyzer);

        Assert.Empty(ids);
    }

    [Fact]
    internal void The_rule_is_a_warning_in_the_usage_category_with_a_help_link_to_its_page()
    {
        var rule = Assert.Single(Analyzer.SupportedDiagnostics);

        Assert.Multiple(
            () => Assert.Equal("WYZ0111", rule.Id),
            () => Assert.Equal(Microsoft.CodeAnalysis.DiagnosticSeverity.Warning, rule.DefaultSeverity),
            () => Assert.Equal("Usage", rule.Category),
            () => Assert.EndsWith("/docs/result/analyzers/WYZ0111.md", rule.HelpLinkUri, StringComparison.Ordinal));
    }
}
