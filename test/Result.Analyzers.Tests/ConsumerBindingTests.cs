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
    internal void A_null_argument_is_ambiguous_for_ValueOr_on_a_task_of_a_result()
    {
        var errors = AnalyzerTestHost.CompilerErrors("Task<string> M(Task<Result<string>> r) => r.ValueOr(null!);");

        Assert.Equal(["CS0121"], errors);
    }

    // A null or default literal converts to the value as well as to a task. When the receiver fixes the type of what a function returns,
    // the call must not pick the overload whose function returns a task: it has to be written with a type, so the compiler reports it as ambiguous.
    [Theory]
    [InlineData("Task<string> M(Task<Result<string>> r) => r.ValueOr(() => null!);")]
    [InlineData("Task<int> M(Task<Result<int>> r) => r.ValueOr(() => default);")]
    [InlineData("Task<string> M(Task<Result<string>> r) => r.ValueOr(_ => null!);")]
    [InlineData("Task<int> M(Task<Result<int>> r) => r.ValueOr(default);")]
    [InlineData("string M(Result<string> r) => r.ValueOr(() => null!);")]
    [InlineData("string M(Result<string> r) => r.ValueOr(_ => null!);")]
    [InlineData("Result<int> M(Result<int> r) => r.OrElse(() => default);")]
    [InlineData("Result<int> M(Result<int> r) => r.OrElse(_ => default);")]
    [InlineData("Task<Result<int>> M(Task<Result<int>> r) => r.OrElse(() => default);")]
    [InlineData("Result<int> M(Result<int> r) => r.Ensure(x => default, Error.Create(\"Rejected.\"));")]
    [InlineData("Result<int> M(Result<int> r) => r.EnsureNot(x => default, Error.Create(\"Rejected.\"));")]
    internal void A_null_or_default_literal_is_ambiguous_where_the_receiver_fixes_the_type_of_the_value(string method)
    {
        var errors = AnalyzerTestHost.CompilerErrors(method);

        Assert.Equal(["CS0121"], errors);
    }

    // An asynchronous function still binds to the overload that takes a function that returns a task, and a function with a type binds to its own.
    [Theory]
    [InlineData("Task<int> M(Task<Result<int>> r) => r.ValueOr(async () => { await Task.Yield(); return 5; });")]
    [InlineData("Task<int> M(Result<int> r) => r.ValueOr(async _ => { await Task.Yield(); return 5; });")]
    [InlineData("ValueTask<int> M(Result<int> r) => r.ValueOr(() => new ValueTask<int>(5));")]
    [InlineData("Task<Result<int>> M(Result<int> r) => r.OrElse(async () => { await Task.Yield(); return 5; });")]
    [InlineData("Task<Result<int>> M(Result<int> r) => r.OrElse(async _ => { await Task.Yield(); return Result.Success(5); });")]
    [InlineData("Task<Result<int>> M(Result<int> r) => r.Ensure(async x => { await Task.Yield(); return true; }, Error.Create(\"Rejected.\"));")]
    [InlineData("int M(Result<int> r) => r.ValueOr(() => 5);")]
    [InlineData("string M(Result<string> r) => r.ValueOr((string)null!);")]
    [InlineData("Task<string> M(Task<Result<string>> r) => r.ValueOr((string)null!);")]
    [InlineData("Task<int> M(Task<Result<int>> r) => r.ValueOr(default(int));")]
    [InlineData("Result<int> M(Result<int> r) => r.Ensure(x => x > 1, Error.Create(\"Rejected.\"));")]
    internal void A_function_that_does_not_depend_on_a_literal_still_binds(string method)
    {
        var errors = AnalyzerTestHost.CompilerErrors(method);

        Assert.Empty(errors);
    }

    [Fact]
    internal void A_lambda_that_returns_a_value_binds_to_OrElse_on_a_result_of_int()
    {
        var warnings = AnalyzerTestHost.NullabilityWarnings("Result<int> M(Result<int> r) => r.OrElse(() => 5);");

        Assert.Empty(warnings);
    }
}
