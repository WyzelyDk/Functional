using System.Reflection;

namespace Wyzely.Functional.MaybeTests;

// Unlike Result, a Maybe converts from a plain value on purpose: absence is what null means, so a null reference becomes none and
// `return user;` in a method that returns Maybe<User> does the right thing. This is the only conversion, and it never goes the other way.
public sealed class ImplicitConversionTests
{
    [Fact]
    internal void The_only_implicit_conversion_wraps_a_value_in_a_maybe()
    {
        var conversions = typeof(Maybe<int>)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Where(method => method.Name == "op_Implicit")
            .Select(method => method.GetParameters()[0].ParameterType.Name + " -> " + method.ReturnType.Name)
            .ToList();

        Assert.Equal(["Int32 -> Maybe`1"], conversions);
    }

    [Fact]
    internal void No_conversion_leads_out_of_a_maybe()
    {
        var conversions = typeof(Maybe<int>)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Where(method => method.Name is "op_Implicit" or "op_Explicit")
            .Where(method => method.GetParameters()[0].ParameterType == typeof(Maybe<int>));

        Assert.Empty(conversions);
    }

    [Fact]
    internal void A_value_returned_from_a_method_becomes_some_and_null_becomes_none()
    {
        Assert.Multiple(
            () => Assert.Equal("ada", Find("ada").GetValue()),
            () => Assert.True(Find(null).IsNone));
    }

    private static Maybe<string> Find(string? name) => name;
}
