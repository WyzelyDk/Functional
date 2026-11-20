using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace Wyzely.Functional.Internal;

/// <summary>
/// What the JSON guards of the result, maybe and error types have in common: the refusal with its message, and the converter that a guard for a generic
/// type hands out. A guard refuses to write or read a type whose default JSON form loses information or exposes it, so that the mistake is an exception
/// at the first use and not wrong data later. A converter that a caller registers in the options takes the place of it.
/// </summary>
internal static class JsonRefusal
{
    /// <summary>Creates the exception that says why the type has no JSON form and what to do instead.</summary>
    /// <param name="type">The type that is refused.</param>
    /// <param name="reason">Why the type has no JSON form, as a sentence.</param>
    /// <param name="page">The folder of the documentation page that explains it (<c>result</c> or <c>maybe</c>).</param>
    /// <returns>The exception to throw.</returns>
    internal static NotSupportedException Create(Type type, string reason, string page)
    {
        var address = $"https://github.com/WyzelyDk/Functional/blob/main/docs/{page}/safety-and-analyzers.md#serialization";
        var name = DisplayName(type);
        var message = $"{name} is not meant to be serialized: {reason} " +
            "Map it to a type of your own first (a DTO, for example), or register your own JsonConverter in the JsonSerializerOptions. " +
            $"See {address}";

        return new NotSupportedException(message);
    }

    /// <summary>Tells whether a type is a closed form of a generic type, such as <c>Maybe&lt;int&gt;</c> of <c>Maybe&lt;&gt;</c>.</summary>
    /// <param name="type">The type to test.</param>
    /// <param name="genericTypeDefinition">The generic type, such as <c>typeof(Maybe&lt;&gt;)</c>.</param>
    /// <returns><see langword="true"/> when the type is a closed form of the generic type.</returns>
    internal static bool IsClosedFormOf(Type type, Type genericTypeDefinition) =>
        type.IsGenericType && type.GetGenericTypeDefinition() == genericTypeDefinition;

    /// <summary>Creates the converter that refuses a closed form of a generic type.</summary>
    /// <param name="type">The closed generic type, such as <c>Maybe&lt;int&gt;</c>.</param>
    /// <param name="reason">Why the type has no JSON form, as a sentence.</param>
    /// <param name="page">The folder of the documentation page that explains it (<c>result</c> or <c>maybe</c>).</param>
    /// <returns>A converter that throws on every write and read.</returns>
    /// <exception cref="NotSupportedException">The converter cannot be created (Native AOT), which is the refusal itself.</exception>
    [UnconditionalSuppressMessage(
        "AOT",
        "IL3050",
        Justification = "The converter only throws; a type that the AOT compiler did not instantiate fails with an exception too.")]
    internal static JsonConverter CreateConverter(Type type, string reason, string page)
    {
        try
        {
            var converterType = typeof(RefusingJsonConverter<>).MakeGenericType(type);

            return (JsonConverter)Activator.CreateInstance(converterType, reason, page)!;
        }
        catch (Exception)
        {
            // Whatever stops the converter from being created (Native AOT has no code for a closed generic type that the program never named),
            // the refusal must still say what it is.
            throw Create(type, reason, page);
        }
    }

    private static string DisplayName(Type type)
    {
        if (!type.IsGenericType)
        {
            return type.Name;
        }

        var name = type.Name[..type.Name.IndexOf('`', StringComparison.Ordinal)];
        var names = type.GetGenericArguments().Select(static argument => argument.Name);
        var arguments = string.Join(", ", names);

        return $"{name}<{arguments}>";
    }
}
