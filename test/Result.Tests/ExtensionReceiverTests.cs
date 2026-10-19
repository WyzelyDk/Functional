using System.Reflection;
using System.Runtime.CompilerServices;

namespace Wyzely.Functional.ResultTests;

// The receivers are passed by value on purpose. `in` looked attractive for a large Result<T>, but a chain passes the result of the
// previous call, a temporary, and taking its address forces it into memory: measured, `in` made a four-step chain several times
// slower (see the ParameterPassing benchmark and the README). This test keeps someone from "optimising" it back.
public sealed class ExtensionReceiverTests
{
    [Fact]
    internal void Typed_receivers_are_passed_by_value()
    {
        var receivers = ReceiverParameters().Where(parameter => IsGeneric(Unwrap(parameter), typeof(Result<>))).ToList();

        Assert.Multiple(
            () => Assert.NotEmpty(receivers),
            () => Assert.All(receivers, parameter => Assert.False(parameter.ParameterType.IsByRef, parameter.Member.ToString())));
    }

    [Fact]
    internal void Untyped_receivers_are_passed_by_value()
    {
        var receivers = ReceiverParameters().Where(parameter => Unwrap(parameter) == typeof(Result)).ToList();

        Assert.Multiple(
            () => Assert.NotEmpty(receivers),
            () => Assert.All(receivers, parameter => Assert.False(parameter.ParameterType.IsByRef, parameter.Member.ToString())));
    }

    private static IEnumerable<ParameterInfo> ReceiverParameters() => typeof(ResultExtensions)
        .GetMethods(BindingFlags.Public | BindingFlags.Static)
        .Where(method => method.IsDefined(typeof(ExtensionAttribute), inherit: false))
        .Select(method => method.GetParameters()[0]);

    private static Type Unwrap(ParameterInfo parameter) =>
        parameter.ParameterType.IsByRef ? parameter.ParameterType.GetElementType()! : parameter.ParameterType;

    private static bool IsGeneric(Type type, Type definition) => type.IsGenericType && type.GetGenericTypeDefinition() == definition;
}
