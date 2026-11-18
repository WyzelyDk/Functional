using System.Reflection;

namespace Wyzely.Functional.ArgumentTesting;

// The convention for the names of delegate parameters: a delegate says what it does or that it makes something, and never carries the name of what it returns.
// A function that creates an error is errorFactory, not error; one that computes a fallback is fallbackFactory; and a gate that is a function of the value
// or of the error is a predicate, while a plain bool or a function without parameters is the condition.
internal static class DelegateParameterNames
{
    private const BindingFlags PublicMembers = BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly;

    private static readonly string[] NamesOfWhatIsProduced = ["error", "message", "exception", "fallback", "replacement"];

    // The parameters that break the convention, one text per overload: the member, the parameter and what to do.
    internal static IReadOnlyList<string> Violations(IEnumerable<Type> types) =>
    [
        .. DelegateParameters(types).Select(static found => Check(found.Member, found.Parameter)).OfType<string>(),
    ];

    // The names of all delegate parameters, so that a test can show that the names it expects exist.
    internal static IReadOnlySet<string> Names(IEnumerable<Type> types) =>
        DelegateParameters(types).Select(static found => found.Parameter.Name!).ToHashSet(StringComparer.Ordinal);

    private static IEnumerable<(MethodBase Member, ParameterInfo Parameter)> DelegateParameters(IEnumerable<Type> types)
    {
        var members = types
            .SelectMany(static type => type.GetMethods(PublicMembers).Cast<MethodBase>().Concat(type.GetConstructors()))
            .Where(static member => member is not MethodInfo { IsSpecialName: true, Name: not ("op_Implicit" or "op_Explicit") });

        foreach (var member in members)
        {
            foreach (var parameter in member.GetParameters().Where(static parameter => typeof(Delegate).IsAssignableFrom(parameter.ParameterType)))
            {
                yield return (member, parameter);
            }
        }
    }

    private static string? Check(MethodBase member, ParameterInfo parameter)
    {
        var where = $"{member.DeclaringType?.Name}.{member.Name}, parameter '{parameter.Name}'";

        if (NamesOfWhatIsProduced.Contains(parameter.Name))
        {
            return $"{where}: a delegate is not named like what it produces; end the name with Factory";
        }

        var takesAParameter = parameter.ParameterType.GetMethod("Invoke")!.GetParameters().Length > 0;

        return parameter.Name == "condition" && takesAParameter
            ? $"{where}: a gate that is a function of a value is called predicate; condition is for a bool and for a function without parameters"
            : null;
    }
}
