using System.Text.Json;
using System.Text.Json.Serialization;

namespace Wyzely.Functional.Internal;

/// <summary>
/// Refuses to write or read a result without a value as JSON, with a message that says why and what to do instead (see <see cref="JsonRefusal"/>).
/// Public only so that a source-generated serializer context can reach it from the attribute; it is not meant to be used directly.
/// </summary>
public sealed class ResultJsonGuard : JsonConverter<Result>
{
    /// <summary>Gets a value indicating whether a <c>null</c> in the JSON is given to this converter too, which it is: a struct cannot be null.</summary>
    public override bool HandleNull => true;

    /// <inheritdoc />
    public override Result Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        throw JsonRefusal.Create(typeof(Result), JsonGuardReasons.Result, "result");

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, Result value, JsonSerializerOptions options)
    {
        Guard.NotNull(writer, nameof(writer));
        Guard.NotNull(options, nameof(options));

        throw JsonRefusal.Create(typeof(Result), JsonGuardReasons.Result, "result");
    }
}
