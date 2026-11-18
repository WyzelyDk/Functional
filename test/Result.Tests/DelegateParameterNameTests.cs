using Wyzely.Functional.ArgumentTesting;

namespace Wyzely.Functional.ResultTests;

// A delegate says what it does or that it makes something. It never carries the name of what it returns: errorFactory, not error. A gate that is a function
// of a value or of an error is a predicate; a bool and a function without parameters is the condition.
public sealed class DelegateParameterNameTests
{
    private static readonly Type[] Types = [.. typeof(Result).Assembly.GetExportedTypes()];

    [Fact]
    internal void No_delegate_parameter_is_named_like_what_it_produces()
    {
        var violations = DelegateParameterNames.Violations(Types);

        var message = $"{violations.Count} parameter(s) break the naming convention:{Environment.NewLine}"
            + string.Join(Environment.NewLine, violations.Take(30));

        Assert.True(violations.Count == 0, message);
    }

    [Fact]
    internal void The_names_of_the_convention_exist()
    {
        var names = DelegateParameterNames.Names(Types);

        Assert.Multiple(
            () => Assert.Contains("errorFactory", names),
            () => Assert.Contains("messageFactory", names),
            () => Assert.Contains("exceptionFactory", names),
            () => Assert.Contains("fallbackFactory", names),
            () => Assert.Contains("replacementFactory", names),
            () => Assert.Contains("predicate", names),
            () => Assert.Contains("condition", names));
    }

    [Fact]
    internal void ValueOr_takes_a_value_as_fallback_and_a_function_as_fallbackFactory()
    {
        var failed = Result.Failure<int>("No.");

        var value = failed.ValueOr(fallback: 7);
        var computed = failed.ValueOr(fallbackFactory: static () => 8);
        var fromError = failed.ValueOr(fallbackFactory: static error => error.Message.Length);

        Assert.Multiple(
            () => Assert.Equal(7, value),
            () => Assert.Equal(8, computed),
            () => Assert.Equal(3, fromError));
    }

    [Fact]
    internal void OrElse_takes_a_function_that_computes_a_value_as_replacementFactory()
    {
        var failed = Result.Failure<int>("No.");

        var computed = failed.OrElse(replacementFactory: static () => 5);
        var fromError = failed.OrElse(replacementFactory: static error => error.Message.Length);

        Assert.Multiple(
            () => Assert.Equal(5, computed.GetValue()),
            () => Assert.Equal(3, fromError.GetValue()));
    }

    [Fact]
    internal void A_gate_that_is_a_function_of_the_value_or_of_the_error_is_called_predicate()
    {
        var mapped = Result.Success(2).MapIf(predicate: static value => value > 1, map: static value => value + 1);
        var seen = new List<string>();
        var failed = Result.Failure<int>("No.");

        failed.TapErrorIf(predicate: static error => error.Message.Length > 1, tapError: error => seen.Add(error.Message));

        Assert.Multiple(
            () => Assert.Equal(3, mapped.GetValue()),
            () => Assert.Equal(["No."], seen));
    }
}
