using System.Reflection;

namespace Wyzely.Functional.ResultTests;

public sealed class ResultAccessTests
{
    private static readonly Error Boom = Error.Create("Boom.");

    [Fact]
    internal void GetError_returns_the_error_of_a_failed_result()
    {
        Assert.Same(Boom, Result.Failure(Boom).GetError());
    }

    [Fact]
    internal void GetError_throws_when_the_result_succeeded()
    {
        Assert.Throws<InvalidOperationException>(() => Result.Success().GetError());
    }

    [Fact]
    internal void GetError_throws_for_a_default_result_because_it_succeeded()
    {
        Assert.Throws<InvalidOperationException>(() => default(Result).GetError());
    }

    [Fact]
    internal void GetError_of_a_typed_result_returns_the_error_of_a_failed_result()
    {
        Assert.Same(Boom, Result.Failure<int>(Boom).GetError());
    }

    [Fact]
    internal void GetError_of_a_typed_result_throws_when_the_result_succeeded()
    {
        Assert.Throws<InvalidOperationException>(() => Result.Success(1).GetError());
    }

    [Fact]
    internal void GetError_of_a_typed_result_throws_for_a_default_result_because_it_succeeded()
    {
        Assert.Throws<InvalidOperationException>(() => default(Result<int>).GetError());
    }

    [Fact]
    internal void GetValue_returns_the_value_of_a_successful_result()
    {
        Assert.Equal(42, Result.Success(42).GetValue());
    }

    [Fact]
    internal void GetValue_throws_with_the_error_when_the_result_failed()
    {
        var exception = Assert.Throws<ResultFailedException>(() => Result.Failure<int>(Boom).GetValue());

        Assert.Same(Boom, exception.Error);
    }

    [Fact]
    internal void GetValue_returns_the_default_value_for_a_default_result()
    {
        Assert.Equal(0, default(Result<int>).GetValue());
    }

    [Theory]
    [InlineData(typeof(Result), "Error")]
    [InlineData(typeof(Result<int>), "Error")]
    [InlineData(typeof(Result<int>), "Value")]
    internal void The_public_surface_has_no_property_that_throws_for_the_wrong_state(Type type, string name)
    {
        var property = type.GetProperty(name, BindingFlags.Public | BindingFlags.Instance);

        Assert.Null(property);
    }
}
