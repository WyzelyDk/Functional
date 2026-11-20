using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Wyzely.Functional.Internal;

/// <summary>
/// Refuses to write or read a result with a value as JSON, with a message that says why and what to do instead (see <see cref="JsonRefusal"/>).
/// Public only so that a source-generated serializer context can reach it from the attribute; it is not meant to be used directly.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class ResultOfTJsonGuard : JsonConverterFactory
{
    /// <inheritdoc />
    public override bool CanConvert(Type typeToConvert)
    {
        Guard.NotNull(typeToConvert, nameof(typeToConvert));

        return JsonRefusal.IsClosedFormOf(typeToConvert, typeof(Result<>));
    }

    /// <inheritdoc />
    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        Guard.NotNull(typeToConvert, nameof(typeToConvert));
        Guard.NotNull(options, nameof(options));

        return JsonRefusal.CreateConverter(typeToConvert, JsonGuardReasons.Result, "result");
    }
}
