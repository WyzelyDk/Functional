using System.Text.Json;
using System.Text.Json.Serialization;

namespace Wyzely.Functional.Internal;

/// <summary>
/// The converter that a guard for a generic type hands out for a closed form of it (see <see cref="JsonRefusal"/>). It writes and reads nothing.
/// </summary>
/// <typeparam name="T">The struct that is refused.</typeparam>
/// <param name="reason">Why the type has no JSON form, as a sentence.</param>
/// <param name="page">The folder of the documentation page that explains it (<c>result</c> or <c>maybe</c>).</param>
internal sealed class RefusingJsonConverter<T>(string reason, string page) : JsonConverter<T>
{
    /// <summary>
    /// Gets a value indicating whether a <c>null</c> in the JSON is given to this converter too, which it is: the serializer would refuse it with
    /// another message, and a struct cannot be null.
    /// </summary>
    public override bool HandleNull => true;

    /// <inheritdoc />
    public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) => throw JsonRefusal.Create(typeof(T), reason, page);

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options) => throw JsonRefusal.Create(typeof(T), reason, page);
}
