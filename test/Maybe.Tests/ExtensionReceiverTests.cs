using System.Reflection;
using System.Runtime.CompilerServices;

namespace Wyzely.Functional.MaybeTests;

// The receivers are passed by value on purpose: a chain passes the maybe returned by the previous call, a temporary, and taking its
// address (`in`) forces it into memory. The same was measured for Result (see its ParameterPassing benchmark); this test keeps someone
// from "optimising" it back.
public sealed class ExtensionReceiverTests
{
    [Fact]
    internal void Receivers_are_passed_by_value()
    {
        var receivers = ReceiverParameters().Where(parameter => IsGeneric(Unwrap(parameter), typeof(Maybe<>))).ToList();

        Assert.Multiple(
            () => Assert.NotEmpty(receivers),
            () => Assert.All(receivers, parameter => Assert.False(parameter.ParameterType.IsByRef, parameter.Member.ToString())));
    }

    private static IEnumerable<ParameterInfo> ReceiverParameters() => typeof(MaybeExtensions)
        .GetMethods(BindingFlags.Public | BindingFlags.Static)
        .Where(method => method.IsDefined(typeof(ExtensionAttribute), inherit: false))
        .Select(method => method.GetParameters()[0]);

    private static Type Unwrap(ParameterInfo parameter) =>
        parameter.ParameterType.IsByRef ? parameter.ParameterType.GetElementType()! : parameter.ParameterType;

    private static bool IsGeneric(Type type, Type definition) => type.IsGenericType && type.GetGenericTypeDefinition() == definition;
}
