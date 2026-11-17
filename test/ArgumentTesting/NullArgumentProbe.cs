using System.Linq.Expressions;
using System.Reflection;

namespace Wyzely.Functional.ArgumentTesting;

// Calls every public static method, instance method, constructor, property setter and conversion operator of the given types once for every
// parameter that must not be null, with that argument set to null and the others valid, and reports what did not throw an ArgumentNullException
// for that parameter at the call.
// A method that returns a task has to throw before it returns it: a task that is faulted with the exception later, or a value returned without
// an exception, is reported too.
internal static class NullArgumentProbe
{
    private const BindingFlags PublicMembers = BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly;
    private static readonly TimeSpan TaskTimeout = TimeSpan.FromSeconds(5);

    internal static ProbeReport Probe(IEnumerable<Type> types)
    {
        var probed = 0;
        var violations = new List<string>();
        var unsupported = new List<string>();

        foreach (var type in types)
        {
            foreach (var member in ProbeableMembers(type))
            {
                probed += ProbeMember(type, member, violations, unsupported);
            }
        }

        return new ProbeReport(probed, violations, unsupported);
    }

    private static IEnumerable<MethodBase> ProbeableMembers(Type type)
    {
        var methods = type.GetMethods(PublicMembers).Where(IsProbeableName);
        var constructors = type.GetConstructors(BindingFlags.Public | BindingFlags.Instance);

        var members = constructors.Cast<MethodBase>().Concat(methods);

        return members.Where(static member => member.GetParameters().All(static parameter => !parameter.ParameterType.IsByRef));
    }

    // Ordinary methods, property setters and conversion operators; the other special methods (getters, comparison operators, events) take none.
    private static bool IsProbeableName(MethodInfo method) =>
        !method.IsSpecialName || method.Name is "op_Implicit" or "op_Explicit" || method.Name.StartsWith("set_", StringComparison.Ordinal);

    private static int ProbeMember(Type declaringType, MethodBase definition, List<string> violations, List<string> unsupported)
    {
        var indexes = NonNullableReferenceParameters(definition);

        if (indexes.Count == 0)
        {
            return 0;
        }

        try
        {
            var closedType = Close(declaringType);
            var member = Instantiate(definition, closedType);
            var instance = member.IsStatic || member.IsConstructor ? null : ValidArgument(closedType);
            var valid = member.GetParameters().Select(static parameter => ValidArgument(parameter.ParameterType)).ToArray();

            foreach (var index in indexes)
            {
                var problem = ProbeParameter(member, instance, valid, index);

                if (problem is not null)
                {
                    violations.Add($"{Describe(definition)}, parameter '{definition.GetParameters()[index].Name}': {problem}");
                }
            }

            return indexes.Count;
        }
        catch (NotSupportedException exception)
        {
            unsupported.Add($"{Describe(definition)}: {exception.Message}");

            return 0;
        }
    }

    // The parameters of the member as declared: a reference type that is not annotated as nullable and is not a bare type parameter.
    private static List<int> NonNullableReferenceParameters(MethodBase definition)
    {
        var context = new NullabilityInfoContext();
        var parameters = definition.GetParameters();
        var indexes = new List<int>();

        for (var index = 0; index < parameters.Length; index++)
        {
            var type = parameters[index].ParameterType;

            if (!type.IsValueType && !type.IsGenericParameter && context.Create(parameters[index]).WriteState == NullabilityState.NotNull)
            {
                indexes.Add(index);
            }
        }

        return indexes;
    }

    private static Type Close(Type type) =>
        type.IsGenericTypeDefinition ? type.MakeGenericType([.. type.GetGenericArguments().Select(ChooseTypeArgument)]) : type;

    // The member as declared on the closed type, with its own type arguments chosen when it is a generic method.
    private static MethodBase Instantiate(MethodBase definition, Type closedType)
    {
        var member = MethodBase.GetMethodFromHandle(definition.MethodHandle, closedType.TypeHandle)!;

        if (member is not MethodInfo { IsGenericMethodDefinition: true } generic)
        {
            return member;
        }

        try
        {
            return generic.MakeGenericMethod([.. generic.GetGenericArguments().Select(ChooseTypeArgument)]);
        }
        catch (ArgumentException exception)
        {
            throw new NotSupportedException("no type arguments satisfy the constraints: " + exception.Message, exception);
        }
    }

    private static Type ChooseTypeArgument(Type parameter)
    {
        var constraints = parameter.GetGenericParameterConstraints();
        var attributes = parameter.GenericParameterAttributes;

        if (attributes.HasFlag(GenericParameterAttributes.NotNullableValueTypeConstraint))
        {
            return typeof(int);
        }

        if (constraints.Length > 0)
        {
            return constraints[0];
        }

        return attributes.HasFlag(GenericParameterAttributes.ReferenceTypeConstraint) ? typeof(string) : typeof(int);
    }

    // Null for the parameter at the index, valid values for the others. Null when the member behaved, otherwise what it did instead.
    private static string? ProbeParameter(MethodBase member, object? instance, object?[] valid, int index)
    {
        var arguments = (object?[])valid.Clone();
        arguments[index] = null;
        var name = member.GetParameters()[index].Name;

        object? returned;

        try
        {
            returned = member is ConstructorInfo constructor ? constructor.Invoke(arguments) : member.Invoke(instance, arguments);
        }
        catch (TargetInvocationException exception)
        {
            return CheckException(exception.InnerException, name, "at the call");
        }

        return CheckReturned(returned, name);
    }

    private static string? CheckException(Exception? exception, string? name, string when) => exception switch
    {
        ArgumentNullException { ParamName: var paramName } when paramName == name => null,
        ArgumentNullException { ParamName: var paramName } => $"threw ArgumentNullException for '{paramName}' {when}",
        _ => $"threw {exception?.GetType().Name} {when}",
    };

    private static string? CheckReturned(object? returned, string? name)
    {
        var task = returned switch
        {
            Task asTask => asTask,
            ValueTask valueTask => valueTask.AsTask(),
            _ when returned is not null && IsGenericValueTask(returned.GetType()) => AsTask(returned),
            _ => null,
        };

        if (task is null)
        {
            return $"returned {returned?.GetType().Name ?? "nothing"} without throwing";
        }

        return CheckFaultedTask(task, name);
    }

    private static string? CheckFaultedTask(Task task, string? name)
    {
        try
        {
            task.Wait(TaskTimeout);
        }
        catch (AggregateException exception)
        {
            var problem = CheckException(exception.InnerException, name, "only when the returned task was awaited");

            return problem ?? "threw only when the returned task was awaited, not at the call";
        }

        return "returned a task that completed without throwing";
    }

    private static bool IsGenericValueTask(Type type) => type.IsGenericType && type.GetGenericTypeDefinition() == typeof(ValueTask<>);

    private static Task AsTask(object valueTask) => (Task)valueTask.GetType().GetMethod(nameof(ValueTask<>.AsTask))!.Invoke(valueTask, null)!;

    // A valid value for a parameter of the type: a default struct, an empty collection, a completed task or a delegate that returns default.
    private static object? ValidArgument(Type type)
    {
        if (type.IsValueType)
        {
            return Nullable.GetUnderlyingType(type) is null ? Activator.CreateInstance(type) : null;
        }

        return type switch
        {
            _ when type == typeof(string) => "x",
            _ when type == typeof(Task) => Task.CompletedTask,
            _ when type == typeof(Exception) => new InvalidOperationException("x"),
            _ when type.IsArray => Array.CreateInstance(type.GetElementType()!, 0),
            _ when typeof(Delegate).IsAssignableFrom(type) => CreateDelegate(type),
            _ when type.IsGenericType => ValidGenericArgument(type),
            _ => CreateObject(type),
        };
    }

    private static object ValidGenericArgument(Type type)
    {
        var definition = type.GetGenericTypeDefinition();
        var argument = type.GetGenericArguments()[0];

        if (definition == typeof(Task<>))
        {
            return typeof(Task).GetMethod(nameof(Task.FromResult))!.MakeGenericMethod(argument).Invoke(null, [ValidArgument(argument)])!;
        }

        if (definition == typeof(IAsyncEnumerable<>))
        {
            var empty = typeof(NullArgumentProbe).GetMethod(nameof(EmptyAsync), BindingFlags.NonPublic | BindingFlags.Static)!;

            return empty.MakeGenericMethod(argument).Invoke(null, null)!;
        }

        if (definition == typeof(IReadOnlyDictionary<,>))
        {
            return Activator.CreateInstance(typeof(Dictionary<,>).MakeGenericType(type.GetGenericArguments()))!;
        }

        var items = Array.CreateInstance(argument, 0);

        return type.IsInstanceOfType(items) ? items : CreateObject(type);
    }

    // An object of a class: through its public constructor without parameters, or through a public static Create method that takes a text.
    private static object CreateObject(Type type)
    {
        var constructor = type.GetConstructor(Type.EmptyTypes);

        if (constructor is not null)
        {
            return constructor.Invoke(null);
        }

        var factory = type.GetMethod("Create", BindingFlags.Public | BindingFlags.Static, [typeof(string)]);

        return factory?.Invoke(null, ["x"]) ?? throw new NotSupportedException("no valid argument for " + type);
    }

    private static Delegate CreateDelegate(Type delegateType)
    {
        var invoke = delegateType.GetMethod(nameof(Action.Invoke))!;
        var parameters = invoke.GetParameters().Select(static parameter => Expression.Parameter(parameter.ParameterType)).ToArray();
        Expression body = invoke.ReturnType == typeof(void) ? Expression.Empty() : Expression.Default(invoke.ReturnType);

        return Expression.Lambda(delegateType, body, parameters).Compile();
    }

    private static async IAsyncEnumerable<T> EmptyAsync<T>()
    {
        await Task.CompletedTask;

        yield break;
    }

    private static string Describe(MethodBase member)
    {
        var parameters = member.GetParameters().Select(static parameter => ShortName(parameter.ParameterType));

        return $"{ShortName(member.DeclaringType!)}.{member.Name}({string.Join(", ", parameters)})";
    }

    private static string ShortName(Type type) =>
        type.IsGenericType ? type.Name.Split('`')[0] + "<" + string.Join(", ", type.GetGenericArguments().Select(ShortName)) + ">" : type.Name;

    // What a probe of a set of types found: how many arguments it set to null, the ones that did not behave, and the members it could not call.
    internal sealed record ProbeReport(int Probed, IReadOnlyList<string> Violations, IReadOnlyList<string> Unsupported)
    {
        // The lists as texts, one entry per line, so that a failed assertion shows all of it.
        internal string UnsupportedText => "Members that the probe could not call:" + Lines(Unsupported);

        internal string ViolationsText => "Arguments that were not rejected at the call:" + Lines(Violations);

        private static string Lines(IReadOnlyList<string> entries) => Environment.NewLine + string.Join(Environment.NewLine, entries);
    }
}
