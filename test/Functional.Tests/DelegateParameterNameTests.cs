using Wyzely.Functional.ArgumentTesting;

namespace Wyzely.Functional.Tests;

// A delegate says what it does or that it makes something. It never carries the name of what it returns: errorFactory, not error.
public sealed class DelegateParameterNameTests
{
    private static readonly Type[] Types = [.. typeof(MaybeResultExtensions).Assembly.GetExportedTypes()];

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

        Assert.Contains("errorFactory", names);
    }

    [Fact]
    internal void A_function_that_creates_the_error_is_called_errorFactory()
    {
        var none = Maybe.None<int>();

        var failed = none.ToResult(errorFactory: static () => Error.NotFound("Missing."));
        var inverted = Maybe.Some(3).ToInvertedResult(errorFactory: static value => Error.Conflict($"Taken: {value}."));

        Assert.Multiple(
            () => Assert.Equal("Missing.", failed.GetError().Message),
            () => Assert.Equal("Taken: 3.", inverted.GetError().Message));
    }
}
