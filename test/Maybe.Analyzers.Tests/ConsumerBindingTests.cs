namespace Wyzely.Functional.MaybeAnalyzersTests;

// How a call binds to the overloads of the library is part of its API: these tests compile consumer code and check what the compiler says.
public sealed class ConsumerBindingTests
{
    [Fact]
    internal void A_lambda_binds_to_ValueOr_on_a_task_of_a_maybe_of_object()
    {
        var errors = AnalyzerTestHost.CompilerErrors("Task<object> M(Task<Maybe<object>> m) => m.ValueOr(() => 5);");

        Assert.Empty(errors);
    }

    [Fact]
    internal void A_lambda_binds_to_ValueOr_on_a_task_of_a_maybe_of_int()
    {
        var errors = AnalyzerTestHost.CompilerErrors("Task<int> M(Task<Maybe<int>> m) => m.ValueOr(() => 5);");

        Assert.Empty(errors);
    }

    [Fact]
    internal void A_value_binds_to_ValueOr_on_a_task_of_a_maybe_of_int()
    {
        var errors = AnalyzerTestHost.CompilerErrors("Task<int> M(Task<Maybe<int>> m) => m.ValueOr(5);");

        Assert.Empty(errors);
    }
}
