using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace Wyzely.Functional.AotSamples;

internal static class AotJson
{
    internal static bool Refuses<T>(T value, Func<JsonTypeInfo<T>> typeInfo)
    {
        try
        {
            var info = typeInfo();
            JsonSerializer.Serialize(value, info);
        }
        catch (NotSupportedException exception)
        {
            return exception.Message.Contains("not meant to be serialized", StringComparison.Ordinal);
        }

        return false;
    }
}

// System.Text.Json refuses the result, maybe and error types. Native AOT needs a source-generated context, so this is how a consumer meets the guard.
[JsonSerializable(typeof(Result<int>))]
[JsonSerializable(typeof(Maybe<int>))]
[JsonSerializable(typeof(Error))]
internal sealed partial class AotJsonContext : JsonSerializerContext;
