using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Wyzely.Functional.Internal;

/// <summary>
/// Refuses to write or read the kind of an error as JSON, with a message that says why and what to do instead (see <see cref="JsonRefusal"/>).
/// Public only so that a source-generated serializer context can reach it from the attribute; it is not meant to be used directly.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class ErrorKindJsonGuard : JsonConverter<ErrorKind>
{
    /// <summary>Gets a value indicating whether a <c>null</c> in the JSON is given to this converter too, which it is: a struct cannot be null.</summary>
    public override bool HandleNull => true;

    /// <inheritdoc />
    public override ErrorKind Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        throw JsonRefusal.Create(typeof(ErrorKind), JsonGuardReasons.ErrorKind, "result");

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, ErrorKind value, JsonSerializerOptions options)
    {
        Guard.NotNull(writer, nameof(writer));
        Guard.NotNull(options, nameof(options));

        throw JsonRefusal.Create(typeof(ErrorKind), JsonGuardReasons.ErrorKind, "result");
    }
}
