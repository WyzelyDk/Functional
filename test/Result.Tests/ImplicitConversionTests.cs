using System.Reflection;

namespace Wyzely.Functional.ResultTests;

// A value converts to a success and an error to a failure, in the way a C# 15 union converts its case types, and on every target framework,
// so that the same source means the same everywhere. A typed result also converts to an untyped one and keeps its state.
public sealed class ImplicitConversionTests
{
    private static readonly Error Boom = Error.Create("Boom.");

    [Fact]
    internal void The_implicit_conversions_are_the_value_the_error_and_the_drop_of_the_value()
    {
        var conversions = new[] { typeof(Result), typeof(Result<int>), typeof(Result<string>), typeof(Error) }
            .SelectMany(type => type.GetMethods(BindingFlags.Public | BindingFlags.Static))
            .Where(method => method.Name == "op_Implicit")
            .Select(method => method.GetParameters()[0].ParameterType.Name + " -> " + method.ReturnType.Name)
            .Order(StringComparer.Ordinal)
            .ToList();

        Assert.Equal(
            [
                "Error -> Result",
                "Error -> Result`1",
                "Error -> Result`1",
                "Int32 -> Result`1",
                "Result`1 -> Result",
                "Result`1 -> Result",
                "String -> Result`1",
            ],
            conversions);
    }

    [Fact]
    internal void A_value_converts_to_a_success_that_holds_it()
    {
        Result<int> result = 42;

        Assert.Multiple(
            () => Assert.True(result.IsSuccess),
            () => Assert.Equal(42, result.GetValue()));
    }

    [Fact]
    internal void A_false_converts_to_a_success_that_holds_false()
    {
        Result<bool> result = false;

        Assert.Multiple(
            () => Assert.True(result.IsSuccess),
            () => Assert.False(result.GetValue()));
    }

    [Fact]
    internal void A_null_reference_converts_to_a_success_that_holds_null()
    {
        string? nothing = null;

        Result<string?> result = nothing;

        Assert.Multiple(
            () => Assert.True(result.IsSuccess),
            () => Assert.Null(result.GetValue()));
    }

    [Fact]
    internal void An_error_converts_to_a_failure_that_carries_it()
    {
        Result<int> result = Boom;

        Assert.Multiple(
            () => Assert.True(result.IsFailure),
            () => Assert.Same(Boom, result.GetError()));
    }

    [Fact]
    internal void An_error_converts_to_an_untyped_failure_that_carries_it()
    {
        Result result = Boom;

        Assert.Multiple(
            () => Assert.True(result.IsFailure),
            () => Assert.Same(Boom, result.GetError()));
    }

    [Fact]
    internal void An_error_converts_to_a_failure_even_when_the_value_type_is_object()
    {
        Result<object> result = Boom;

        Assert.Multiple(
            () => Assert.True(result.IsFailure),
            () => Assert.Same(Boom, result.GetError()));
    }

    [Fact]
    internal void Any_other_object_converts_to_a_success_when_the_value_type_is_object()
    {
        Result<object> result = 5;

        Assert.Multiple(
            () => Assert.True(result.IsSuccess),
            () => Assert.Equal(5, result.GetValue()));
    }

    [Fact]
    internal void A_null_error_does_not_convert()
    {
        Error? missing = null;

        Assert.Multiple(
            () => Assert.Throws<ArgumentNullException>(() => ConvertTyped(missing!)),
            () => Assert.Throws<ArgumentNullException>(() => ConvertUntyped(missing!)));
    }

    [Fact]
    internal void A_lambda_that_returns_a_value_or_an_error_converts_to_the_result_that_the_delegate_returns()
    {
        Assert.Multiple(
            () => Assert.Equal(7, Invoke(static () => 7).GetValue()),
            () => Assert.Same(Boom, Invoke(static () => Boom).GetError()));
    }

    [Fact]
    internal void A_typed_result_converts_to_an_untyped_result_in_the_same_state()
    {
        Result success = Result.Success(1);
        Result failure = Result.Failure<int>("Nope.");

        Assert.Multiple(
            () => Assert.True(success.IsSuccess),
            () => Assert.Equal("Nope.", failure.GetError().Message));
    }

    private static Result<int> Invoke(Func<Result<int>> make) => make();

    private static Result<int> ConvertTyped(Error error) => error;

    private static Result ConvertUntyped(Error error) => error;
}
