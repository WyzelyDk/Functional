using System.Reflection;

namespace Wyzely.Functional.ResultTests;

// A value or an error never becomes a result by accident: `return false;` in a method that returns Result<bool> must not compile as
// a success, so the only implicit conversion left is the lossless one that drops the value of a typed result.
public sealed class ImplicitConversionTests
{
    [Fact]
    internal void The_only_implicit_conversion_drops_the_value_of_a_typed_result()
    {
        var conversions = new[] { typeof(Result), typeof(Result<int>), typeof(Error) }
            .SelectMany(type => type.GetMethods(BindingFlags.Public | BindingFlags.Static))
            .Where(method => method.Name == "op_Implicit")
            .Select(method => method.GetParameters()[0].ParameterType.Name + " -> " + method.ReturnType.Name)
            .ToList();

        Assert.Equal(["Result`1 -> Result"], conversions);
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
}
