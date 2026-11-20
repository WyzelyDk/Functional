using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Wyzely.Functional.Internal;

/// <summary>
/// Refuses to write or read a context frame of an error as JSON, with a message that says why and what to do instead (see <see cref="JsonRefusal"/>).
/// A <c>null</c> reference is not refused: it is no hazard, and stays <c>null</c>.
/// Public only so that a source-generated serializer context can reach it from the attribute; it is not meant to be used directly.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class ErrorFrameJsonGuard : JsonConverter<ErrorFrame>
{
    /// <inheritdoc />
    public override ErrorFrame? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        throw JsonRefusal.Create(typeof(ErrorFrame), JsonGuardReasons.SensitiveDetails, "result");

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, ErrorFrame? value, JsonSerializerOptions options)
    {
        Guard.NotNull(writer, nameof(writer));
        Guard.NotNull(options, nameof(options));

        throw JsonRefusal.Create(typeof(ErrorFrame), JsonGuardReasons.SensitiveDetails, "result");
    }
}
