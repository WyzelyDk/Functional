namespace Wyzely.Functional.ResultAnalyzersTests;

// How a call binds to the overloads of the library is part of its API: these tests compile consumer code and check what the compiler says.
public sealed class ConsumerBindingTests
{
    [Fact]
    internal void A_lambda_that_returns_a_value_is_ambiguous_for_OrElse_on_a_result_of_object()
    {
        var errors = AnalyzerTestHost.CompilerErrors("Result<object> M(Result<object> r) => r.OrElse(() => 5);");

        Assert.Equal(["CS0121"], errors);
    }

    [Fact]
    internal void A_lambda_that_returns_a_value_binds_to_OrElse_on_a_result_of_int()
    {
        var warnings = AnalyzerTestHost.NullabilityWarnings("Result<int> M(Result<int> r) => r.OrElse(() => 5);");

        Assert.Empty(warnings);
    }
}
