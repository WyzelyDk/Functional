using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Wyzely.Functional.ApiSurface;

// Describes the public API of an assembly as text, one line per type and one per member, in a stable order, so that a test can compare it with the
// approved text. Everything that a caller can see is in a line: the kind of the type, base type and interfaces, generic constraints (also notnull),
// the names and types of the parameters (a caller may pass them by name), default values, nullable annotations, tuple element names, extension
// methods, and the attributes that change how a call binds or what the compiler knows afterwards ([OverloadResolutionPriority], [NotNullWhen], ...).
internal static class PublicApiDump
{
    private static readonly Dictionary<string, string> Keywords = new(StringComparer.Ordinal)
    {
        ["System.Boolean"] = "bool",
        ["System.Byte"] = "byte",
        ["System.SByte"] = "sbyte",
        ["System.Char"] = "char",
        ["System.Decimal"] = "decimal",
        ["System.Double"] = "double",
        ["System.Single"] = "float",
        ["System.Int16"] = "short",
        ["System.UInt16"] = "ushort",
        ["System.Int32"] = "int",
        ["System.UInt32"] = "uint",
        ["System.Int64"] = "long",
        ["System.UInt64"] = "ulong",
        ["System.IntPtr"] = "nint",
        ["System.UIntPtr"] = "nuint",
        ["System.Object"] = "object",
        ["System.String"] = "string",
        ["System.Void"] = "void",
    };

    // The attributes that the compiler adds for its own purposes. Everything else is shown, so that a new attribute on a public member is noticed.
    private static readonly HashSet<string> CompilerAttributes = new(
    [
        "NullableAttribute",
        "NullableContextAttribute",
        "NullablePublicOnlyAttribute",
        "IsReadOnlyAttribute",
        "IsByRefLikeAttribute",
        "IsUnmanagedAttribute",
        "CompilerGeneratedAttribute",
        "ExtensionAttribute",
        "AsyncStateMachineAttribute",
        "AsyncIteratorStateMachineAttribute",
        "IteratorStateMachineAttribute",
        "DebuggerHiddenAttribute",
        "DebuggerStepThroughAttribute",
        "DebuggerNonUserCodeAttribute",
        "DefaultMemberAttribute",
        "TupleElementNamesAttribute",
        "ParamArrayAttribute",
        "EmbeddedAttribute",
        "RefSafetyRulesAttribute",
        "OutAttribute",
        "InAttribute",
        "OptionalAttribute",
        "DefaultParameterValueAttribute",
    ],
    StringComparer.Ordinal);

    // The public API of an assembly: its exported types (nested ones included) and their public and protected members.
    internal static string Of(Assembly assembly) => OfTypes(assembly.GetExportedTypes());

    internal static string OfTypes(IEnumerable<Type> types)
    {
        var context = new NullabilityInfoContext();
        var lines = new List<string>();

        foreach (var type in types.OrderBy(static type => type.FullName, StringComparer.Ordinal))
        {
            lines.Add(TypeLine(type));
            lines.AddRange(MemberLines(type, context).Order(StringComparer.Ordinal).Select(static line => "  " + line));
        }

        return string.Join('\n', lines) + "\n";
    }

    private static string TypeLine(Type type)
    {
        var inherited = InheritedTypes(type).Select(static inherited => TypeName(inherited, null, null)).Order(StringComparer.Ordinal).ToList();
        var inheritance = inherited.Count == 0 ? string.Empty : " : " + string.Join(", ", inherited);
        var constraints = type.IsGenericTypeDefinition ? Constraints(type.GetGenericArguments()) : string.Empty;

        return $"{Attributes(type.GetCustomAttributesData())}{Kind(type)} {DefinitionName(type)}{constraints}{inheritance}";
    }

    // The base type, and the interfaces that this type adds to those of its base type. The interfaces of System.Enum, System.ValueType and the like
    // are not the type's own and can change with the runtime, so they are left out.
    private static IEnumerable<Type> InheritedTypes(Type type)
    {
        var ignored = new[] { typeof(object), typeof(ValueType), typeof(Enum), typeof(MulticastDelegate) };
        var baseType = type.BaseType is { } found && !ignored.Contains(found) ? new[] { found } : [];
        var inherited = type.BaseType?.GetInterfaces() ?? [];

        return baseType.Concat(type.GetInterfaces().Except(inherited));
    }

    private static string Kind(Type type)
    {
        if (type.IsEnum)
        {
            return "enum";
        }

        if (type.IsInterface)
        {
            return "interface";
        }

        if (type.BaseType == typeof(MulticastDelegate))
        {
            return "delegate";
        }

        return type.IsValueType ? ValueTypeKind(type) : ClassKind(type);
    }

    private static string ValueTypeKind(Type type)
    {
        var readOnly = type.GetCustomAttributesData().Any(static data => data.AttributeType.Name == "IsReadOnlyAttribute");

        return readOnly ? "readonly struct" : "struct";
    }

    private static string ClassKind(Type type)
    {
        if (type.IsAbstract && type.IsSealed)
        {
            return "static class";
        }

        if (type.IsAbstract)
        {
            return "abstract class";
        }

        return type.IsSealed ? "sealed class" : "class";
    }

    private static IEnumerable<string> MemberLines(Type type, NullabilityInfoContext context)
    {
        const BindingFlags all = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly;

        foreach (var member in type.GetMembers(all))
        {
            var line = MemberLine(member, context);

            if (line is not null)
            {
                yield return line;
            }
        }
    }

    private static string? MemberLine(MemberInfo member, NullabilityInfoContext context) => member switch
    {
        ConstructorInfo constructor when IsVisible(constructor) => ConstructorLine(constructor, context),
        MethodInfo method when IsVisible(method) && !IsAccessor(method) => MethodLine(method, context),
        PropertyInfo property when IsVisible(property) => PropertyLine(property, context),
        FieldInfo field when IsVisible(field) && !field.IsSpecialName => FieldLine(field, context),
        EventInfo eventInfo when eventInfo.AddMethod is { } add && IsVisible(add) => EventLine(eventInfo, context),
        _ => null,
    };

    private static bool IsVisible(MethodBase method) => method.IsPublic || method.IsFamily || method.IsFamilyOrAssembly;

    private static bool IsVisible(FieldInfo field) => field.IsPublic || field.IsFamily || field.IsFamilyOrAssembly;

    private static bool IsVisible(PropertyInfo property) =>
        (property.GetMethod is { } getter && IsVisible(getter)) || (property.SetMethod is { } setter && IsVisible(setter));

    private static bool IsAccessor(MethodInfo method) => method.IsSpecialName && !method.Name.StartsWith("op_", StringComparison.Ordinal);

    private static string ConstructorLine(ConstructorInfo constructor, NullabilityInfoContext context) =>
        $"{Attributes(constructor.GetCustomAttributesData())}{(constructor.IsStatic ? "static " : string.Empty)}ctor({Parameters(constructor, context)})";

    private static string MethodLine(MethodInfo method, NullabilityInfoContext context)
    {
        var names = method.IsGenericMethodDefinition ? method.GetGenericArguments().Select(static argument => argument.Name) : [];
        var generic = method.IsGenericMethodDefinition ? "<" + string.Join(", ", names) + ">" : string.Empty;
        var constraints = method.IsGenericMethodDefinition ? Constraints(method.GetGenericArguments()) : string.Empty;
        var returns = ReturnType(method, context);
        var attributes = Attributes(method.GetCustomAttributesData());

        return $"{attributes}{MethodModifiers(method)}{returns} {method.Name}{generic}({Parameters(method, context)}){constraints}";
    }

    private static string ReturnType(MethodInfo method, NullabilityInfoContext context)
    {
        var parameter = method.ReturnParameter;
        var info = context.Create(parameter);
        var names = TupleNames(parameter.GetCustomAttributesData());
        var returnAttributes = Attributes(parameter.GetCustomAttributesData(), "return: ");

        return returnAttributes + TypeName(parameter.ParameterType, info, names);
    }

    private static string MethodModifiers(MethodInfo method)
    {
        var interfaceMember = method.DeclaringType?.IsInterface == true;
        var modifiers = new List<string>();

        if (method.IsStatic)
        {
            modifiers.Add("static");
        }
        else if (!interfaceMember)
        {
            modifiers.AddRange(InstanceModifiers(method));
        }

        return modifiers.Count == 0 ? string.Empty : string.Join(' ', modifiers) + " ";
    }

    private static IEnumerable<string> InstanceModifiers(MethodInfo method)
    {
        if (method.IsAbstract)
        {
            yield return "abstract";
        }
        else if (method.IsVirtual && method.GetBaseDefinition().DeclaringType != method.DeclaringType)
        {
            yield return method.IsFinal ? "sealed override" : "override";
        }
        else if (method.IsVirtual && !method.IsFinal)
        {
            yield return "virtual";
        }
    }

    private static string PropertyLine(PropertyInfo property, NullabilityInfoContext context)
    {
        var info = context.Create(property);
        var names = TupleNames(property.GetCustomAttributesData());
        var indexes = property.GetIndexParameters();
        var index = indexes.Length == 0 ? string.Empty : "[" + string.Join(", ", indexes.Select(parameter => Parameter(parameter, context))) + "]";
        var name = indexes.Length == 0 ? property.Name : "this" + index;
        var modifiers = MethodModifiers((property.GetMethod ?? property.SetMethod)!);

        var type = TypeName(property.PropertyType, info, names);

        return $"{Attributes(property.GetCustomAttributesData())}{modifiers}property {type} {name} {{ {Accessors(property)}}}";
    }

    private static string Accessors(PropertyInfo property)
    {
        var parts = new List<string>();

        if (property.GetMethod is { } getter && IsVisible(getter))
        {
            parts.Add("get;");
        }

        if (property.SetMethod is { } setter && IsVisible(setter))
        {
            parts.Add(IsInitOnly(setter) ? "init;" : "set;");
        }

        return string.Join(' ', parts) + " ";
    }

    private static bool IsInitOnly(MethodInfo setter) =>
        setter.ReturnParameter.GetRequiredCustomModifiers().Any(static modifier => modifier.FullName == "System.Runtime.CompilerServices.IsExternalInit");

    private static string FieldLine(FieldInfo field, NullabilityInfoContext context)
    {
        if (field.DeclaringType!.IsEnum)
        {
            return $"{field.Name} = {Convert.ToString(field.GetRawConstantValue(), CultureInfo.InvariantCulture)}";
        }

        var info = context.Create(field);
        var names = TupleNames(field.GetCustomAttributesData());
        var kind = FieldKind(field);

        return $"{Attributes(field.GetCustomAttributesData())}{kind.Trim()} {TypeName(field.FieldType, info, names)} {field.Name}";
    }

    private static string FieldKind(FieldInfo field)
    {
        if (field.IsLiteral)
        {
            return "const";
        }

        var prefix = field.IsStatic ? "static " : string.Empty;

        return prefix + (field.IsInitOnly ? "readonly" : "field");
    }

    private static string EventLine(EventInfo eventInfo, NullabilityInfoContext context)
    {
        var info = context.Create(eventInfo);

        return $"event {TypeName(eventInfo.EventHandlerType!, info, null)} {eventInfo.Name}";
    }

    private static string Parameters(MethodBase method, NullabilityInfoContext context)
    {
        var extension = method.IsDefined(typeof(ExtensionAttribute), false);
        var parameters = method.GetParameters()
            .Select((parameter, index) => (extension && index == 0 ? "this " : string.Empty) + Parameter(parameter, context));

        return string.Join(", ", parameters);
    }

    private static string Parameter(ParameterInfo parameter, NullabilityInfoContext context)
    {
        var info = context.Create(parameter);
        var names = TupleNames(parameter.GetCustomAttributesData());
        var type = parameter.ParameterType;
        var modifier = ParameterModifier(parameter);
        var attributes = Attributes(parameter.GetCustomAttributesData());
        var core = type.IsByRef ? type.GetElementType()! : type;
        var rendered = TypeName(core, type.IsByRef ? info.ElementType ?? info : info, names, writing: true);
        var isParams = parameter.IsDefined(typeof(ParamArrayAttribute), false) ? "params " : string.Empty;
        var defaultValue = parameter.HasDefaultValue ? " = " + DefaultValue(parameter.DefaultValue) : string.Empty;

        return $"{attributes}{isParams}{modifier}{rendered} {parameter.Name}{defaultValue}";
    }

    private static string ParameterModifier(ParameterInfo parameter)
    {
        if (!parameter.ParameterType.IsByRef)
        {
            return string.Empty;
        }

        if (parameter.IsOut)
        {
            return "out ";
        }

        return parameter.IsIn ? "in " : "ref ";
    }

    private static string DefaultValue(object? value) => value switch
    {
        null => "null",
        string text => "\"" + text + "\"",
        bool flag => flag ? "true" : "false",
        _ => Convert.ToString(value, CultureInfo.InvariantCulture) ?? "null",
    };

    // The name of a type as it is written in C#, with the nullable annotation of this use of it and the names of tuple elements.
    private static string TypeName(Type type, NullabilityInfo? info, Queue<string?>? tupleNames, bool writing = false)
    {
        var mark = IsMarkedNullable(info, writing) ? "?" : string.Empty;

        if (type.IsByRef)
        {
            return TypeName(type.GetElementType()!, info?.ElementType, tupleNames, writing);
        }

        if (type.IsArray)
        {
            return TypeName(type.GetElementType()!, info?.ElementType, tupleNames, writing) + "[]" + mark;
        }

        if (type.IsGenericParameter)
        {
            return type.Name + mark;
        }

        if (Nullable.GetUnderlyingType(type) is { } underlying)
        {
            return TypeName(underlying, null, tupleNames) + "?";
        }

        return type.IsGenericType ? GenericTypeName(type, info, tupleNames, writing) + mark : PlainTypeName(type) + mark;
    }

    private static bool IsMarkedNullable(NullabilityInfo? info, bool writing) =>
        info is not null && (writing ? info.WriteState : info.ReadState) == NullabilityState.Nullable;

    private static string GenericTypeName(Type type, NullabilityInfo? info, Queue<string?>? tupleNames, bool writing)
    {
        var arguments = type.GetGenericArguments();
        var elementNames = IsValueTuple(type) && tupleNames is not null
            ? Enumerable.Range(0, arguments.Length).Select(_ => tupleNames.Count > 0 ? tupleNames.Dequeue() : null).ToArray()
            : null;
        var infos = info?.GenericTypeArguments;
        var rendered = arguments.Select((argument, index) => TypeName(argument, infos is { Length: > 0 } ? infos[index] : null, tupleNames, writing)).ToArray();

        if (elementNames is null)
        {
            return $"{PlainTypeName(type.GetGenericTypeDefinition(), withArity: false)}<{string.Join(", ", rendered)}>";
        }

        var elements = rendered.Select((text, index) => elementNames[index] is { } name ? $"{text} {name}" : text);

        return "(" + string.Join(", ", elements) + ")";
    }

    // FullName is null for a generic type that still contains a type parameter, so the definition is the one that is asked.
    private static bool IsValueTuple(Type type) =>
        type.GetGenericTypeDefinition().FullName?.StartsWith("System.ValueTuple`", StringComparison.Ordinal) == true;

    private static string PlainTypeName(Type type, bool withArity = true)
    {
        var name = (type.FullName ?? type.Name).Replace('+', '.');

        if (Keywords.TryGetValue(name, out var keyword))
        {
            return keyword;
        }

        var tick = name.IndexOf('`', StringComparison.Ordinal);

        return withArity || tick < 0 ? name : name[..tick];
    }

    // The name of a type that is defined here, with its own type parameters (Result<T>).
    private static string DefinitionName(Type type)
    {
        var name = PlainTypeName(type, withArity: false);

        if (!type.IsGenericTypeDefinition)
        {
            return name;
        }

        return name + "<" + string.Join(", ", type.GetGenericArguments().Select(static argument => argument.Name)) + ">";
    }

    private static Queue<string?>? TupleNames(IEnumerable<CustomAttributeData> attributes)
    {
        var data = attributes.FirstOrDefault(static attribute => attribute.AttributeType.Name == "TupleElementNamesAttribute");

        if (data is null || data.ConstructorArguments.Count == 0)
        {
            return null;
        }

        if (data.ConstructorArguments[0].Value is not IReadOnlyCollection<CustomAttributeTypedArgument> values)
        {
            return null;
        }

        return new Queue<string?>(values.Select(static value => value.Value as string));
    }

    private static string Constraints(Type[] parameters)
    {
        var clauses = parameters
            .Select(parameter => (Parameter: parameter, Constraints: ConstraintsOf(parameter)))
            .Where(static pair => pair.Constraints.Count > 0);

        return string.Concat(clauses.Select(static pair => $" where {pair.Parameter.Name} : {string.Join(", ", pair.Constraints)}"));
    }

    private static List<string> ConstraintsOf(Type parameter)
    {
        var flags = parameter.GenericParameterAttributes;
        var constraints = new List<string>();

        if (flags.HasFlag(GenericParameterAttributes.ReferenceTypeConstraint))
        {
            constraints.Add("class");
        }

        if (flags.HasFlag(GenericParameterAttributes.NotNullableValueTypeConstraint))
        {
            constraints.Add(IsUnmanaged(parameter) ? "unmanaged" : "struct");
        }

        var typeConstraints = parameter.GetGenericParameterConstraints().Where(static constraint => constraint.FullName != "System.ValueType");

        constraints.AddRange(typeConstraints.Select(static constraint => TypeName(constraint, null, null)));

        if (flags.HasFlag(GenericParameterAttributes.DefaultConstructorConstraint) && !flags.HasFlag(GenericParameterAttributes.NotNullableValueTypeConstraint))
        {
            constraints.Add("new()");
        }

        if (constraints.Count == 0 && IsNotNull(parameter))
        {
            constraints.Add("notnull");
        }

        return constraints;
    }

    private static bool IsUnmanaged(Type parameter) =>
        parameter.GetCustomAttributesData().Any(static data => data.AttributeType.Name == "IsUnmanagedAttribute");

    // A type parameter with the constraint notnull is "not null" in the nullable context of its declaration, and one without a constraint is "maybe null"
    // ([Nullable(2)]). The compiler writes [Nullable(1)] on the type parameter only where that differs from the [NullableContext] of the method or type.
    private static bool IsNotNull(Type parameter)
    {
        var own = NullableValue(parameter.GetCustomAttributesData(), "NullableAttribute");

        return (own ?? ContextOf(parameter.DeclaringMethod ?? (MemberInfo?)parameter.DeclaringType)) is 1;
    }

    private static byte? ContextOf(MemberInfo? member)
    {
        for (var current = member; current is not null; current = current.DeclaringType)
        {
            if (NullableValue(current.GetCustomAttributesData(), "NullableContextAttribute") is { } context)
            {
                return context;
            }
        }

        return null;
    }

    private static byte? NullableValue(IEnumerable<CustomAttributeData> attributes, string name)
    {
        var data = attributes.FirstOrDefault(attribute => attribute.AttributeType.Name == name);

        if (data is null || data.ConstructorArguments.Count == 0)
        {
            return null;
        }

        var argument = data.ConstructorArguments[0].Value;

        return argument switch
        {
            byte single => single,
            IReadOnlyCollection<CustomAttributeTypedArgument> { Count: > 0 } many when many.First().Value is byte first => first,
            _ => null,
        };
    }

    private static string Attributes(IEnumerable<CustomAttributeData> attributes, string prefix = "")
    {
        var shown = attributes
            .Where(static data => !CompilerAttributes.Contains(data.AttributeType.Name))
            .Select(AttributeText)
            .Order(StringComparer.Ordinal)
            .ToList();

        return shown.Count == 0 ? string.Empty : $"[{prefix}{string.Join(", ", shown)}] ";
    }

    private static string AttributeText(CustomAttributeData data)
    {
        var name = data.AttributeType.Name;
        var shortName = name.EndsWith("Attribute", StringComparison.Ordinal) ? name[..^"Attribute".Length] : name;
        var arguments = data.ConstructorArguments.Select(static argument => ArgumentText(argument))
            .Concat(data.NamedArguments.Select(static named => $"{named.MemberName} = {ArgumentText(named.TypedValue)}"))
            .ToList();

        return arguments.Count == 0 ? shortName : $"{shortName}({string.Join(", ", arguments)})";
    }

    private static string ArgumentText(CustomAttributeTypedArgument argument) => argument.Value switch
    {
        null => "null",
        string text => "\"" + text + "\"",
        bool flag => flag ? "true" : "false",
        Type type => "typeof(" + PlainTypeName(type) + ")",
        IReadOnlyCollection<CustomAttributeTypedArgument> items => "[" + string.Join(", ", items.Select(static item => ArgumentText(item))) + "]",
        var value when argument.ArgumentType.IsEnum => Enum.ToObject(argument.ArgumentType, value).ToString() ?? string.Empty,
        var value => Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty,
    };
}
