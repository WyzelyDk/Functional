namespace Wyzely.Functional.MaybeAnalyzersTests;

// How a call binds to the overloads of the library is part of its API: these tests compile consumer code and check what the compiler says.
public sealed class ConsumerBindingTests
{
    // A null or default literal converts to the value as well as to a task. When the receiver fixes the type of what a function returns,
    // the call must not pick the overload whose function returns a task: it has to be written with a type, so the compiler reports it as ambiguous.
    [Theory]
    [InlineData("Task<string> M(Task<Maybe<string>> m) => m.ValueOr(() => null!);")]
    [InlineData("Task<int> M(Task<Maybe<int>> m) => m.ValueOr(() => default);")]
    [InlineData("Task<string> M(Task<Maybe<string>> m) => m.ValueOr(null!);")]
    [InlineData("Task<int> M(Task<Maybe<int>> m) => m.ValueOr(default);")]
    [InlineData("ValueTask<int> M(ValueTask<Maybe<int>> m) => m.ValueOr(default);")]
    [InlineData("string M(Maybe<string> m) => m.ValueOr(() => null!);")]
    [InlineData("Maybe<int> M(Maybe<int> m) => m.OrElse(() => default);")]
    [InlineData("Task<Maybe<int>> M(Task<Maybe<int>> m) => m.OrElse(() => default);")]
    [InlineData("Maybe<int> M(Maybe<int> m) => m.Filter(x => default);")]
    [InlineData("bool M(Maybe<int> m) => m.Any(x => default);")]
    internal void A_null_or_default_literal_is_ambiguous_where_the_receiver_fixes_the_type_of_the_value(string method)
    {
        var errors = AnalyzerTestHost.CompilerErrors(method);

        Assert.Equal(["CS0121"], errors);
    }

    // An asynchronous function still binds to the overload that takes a function that returns a task, and a function with a type binds to its own.
    [Theory]
    [InlineData("Task<int> M(Task<Maybe<int>> m) => m.ValueOr(async () => { await Task.Yield(); return 5; });")]
    [InlineData("Task<int> M(Maybe<int> m) => m.ValueOr(async () => { await Task.Yield(); return 5; });")]
    [InlineData("ValueTask<int> M(Maybe<int> m) => m.ValueOr(() => new ValueTask<int>(5));")]
    [InlineData("Task<Maybe<int>> M(Maybe<int> m) => m.OrElse(async () => { await Task.Yield(); return 5; });")]
    [InlineData("Task<Maybe<int>> M(Maybe<int> m) => m.OrElse(async () => { await Task.Yield(); return Maybe.Some(5); });")]
    [InlineData("Task<Maybe<int>> M(Maybe<int> m) => m.Filter(async x => { await Task.Yield(); return true; });")]
    [InlineData("Task<bool> M(Maybe<int> m) => m.Any(async x => { await Task.Yield(); return true; });")]
    [InlineData("int M(Maybe<int> m) => m.ValueOr(() => 5);")]
    [InlineData("string M(Maybe<string> m) => m.ValueOr((string)null!);")]
    [InlineData("Task<string> M(Task<Maybe<string>> m) => m.ValueOr((string)null!);")]
    [InlineData("Task<int> M(Task<Maybe<int>> m) => m.ValueOr(default(int));")]
    internal void A_function_that_does_not_depend_on_a_literal_still_binds(string method)
    {
        var errors = AnalyzerTestHost.CompilerErrors(method);

        Assert.Empty(errors);
    }

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
