using Wyzely.Functional.ArgumentTesting;

namespace Wyzely.Functional.MaybeTests;

// A delegate says what it does or that it makes something. It never carries the name of what it returns: fallbackFactory, not fallback.
public sealed class DelegateParameterNameTests
{
    private static readonly Type[] Types = [.. typeof(Maybe).Assembly.GetExportedTypes()];

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
            () => Assert.Contains("fallbackFactory", names),
            () => Assert.Contains("replacementFactory", names),
            () => Assert.Contains("exceptionFactory", names),
            () => Assert.Contains("predicate", names));
    }

    [Fact]
    internal void ValueOr_takes_a_value_as_fallback_and_a_function_as_fallbackFactory()
    {
        var none = Maybe.None<int>();

        var value = none.ValueOr(fallback: 7);
        var computed = none.ValueOr(fallbackFactory: static () => 8);

        Assert.Multiple(
            () => Assert.Equal(7, value),
            () => Assert.Equal(8, computed));
    }

    [Fact]
    internal void OrElse_takes_a_function_that_computes_a_value_as_replacementFactory()
    {
        var none = Maybe.None<int>();

        var computed = none.OrElse(replacementFactory: static () => 5);

        Assert.Equal(5, computed.GetValue());
    }
}
