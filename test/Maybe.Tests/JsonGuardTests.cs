using System.ComponentModel;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Wyzely.Functional.Internal;

namespace Wyzely.Functional.MaybeTests;

// System.Text.Json refuses a maybe, on purpose: written as it is, a maybe loses its value, and there is no one JSON form for "no value" that
// every caller would agree on. The way out is a type of your own, or a converter that you register.
public sealed class JsonGuardTests
{
    [Fact]
    internal void Serializing_a_maybe_with_a_value_throws_and_says_what_to_do_instead()
    {
        var exception = Assert.Throws<NotSupportedException>(() => JsonSerializer.Serialize(Maybe.Some(5)));

        Assert.Multiple(
            () => Assert.Contains("Maybe<Int32>", exception.Message),
            () => Assert.Contains("Map it to a type of your own", exception.Message),
            () => Assert.Contains("register your own JsonConverter", exception.Message),
            () => Assert.Contains("docs/maybe/safety-and-analyzers.md#serialization", exception.Message));
    }

    [Fact]
    internal void Serializing_a_maybe_without_a_value_throws_too()
    {
        Assert.Multiple(
            () => Assert.Throws<NotSupportedException>(() => JsonSerializer.Serialize(Maybe.None<int>())),
            () => Assert.Throws<NotSupportedException>(() => JsonSerializer.Serialize(default(Maybe<string>))));
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("null")]
    [InlineData("5")]
    internal void Deserializing_a_maybe_throws_for_whatever_the_json_is(string json)
    {
        Assert.Throws<NotSupportedException>(() => JsonSerializer.Deserialize<Maybe<int>>(json));
    }

    [Fact]
    internal void A_type_that_holds_a_maybe_cannot_be_serialized_or_deserialized_either()
    {
        var customer = new Customer(Maybe.Some("Ada"));

        Assert.Multiple(
            () => Assert.Throws<NotSupportedException>(() => JsonSerializer.Serialize(customer)),
            () => Assert.Throws<NotSupportedException>(() => JsonSerializer.Deserialize<Customer>("{\"Nickname\":\"Ada\"}")));
    }

    [Fact]
    internal void A_converter_in_the_options_takes_the_place_of_the_guard()
    {
        var options = new JsonSerializerOptions { Converters = { new NullOrValueConverter() } };

        var written = JsonSerializer.Serialize(new[] { Maybe.Some(1), Maybe.None<int>() }, options);
        var read = JsonSerializer.Deserialize<Maybe<int>[]>("[3,null]", options);

        Assert.Multiple(
            () => Assert.Equal("[1,null]", written),
            () => Assert.Equal([Maybe.Some(3), Maybe.None<int>()], read!));
    }

    [Fact]
    internal void The_guard_of_a_generic_type_converts_only_that_generic_type()
    {
        var guard = new MaybeJsonGuard();

        Assert.Multiple(
            () => Assert.True(guard.CanConvert(typeof(Maybe<int>))),
            () => Assert.False(guard.CanConvert(typeof(int))),
            () => Assert.False(guard.CanConvert(typeof(List<int>))));
    }

    [Fact]
    internal void The_guard_of_a_generic_type_still_refuses_with_its_message_when_the_converter_cannot_be_created()
    {
        var guard = new MaybeJsonGuard();

        var exception = Assert.Throws<NotSupportedException>(() => guard.CreateConverter(typeof(int*), new JsonSerializerOptions()));

        Assert.Contains("is not meant to be serialized", exception.Message);
    }

    [Fact]
    internal void The_guards_are_hidden_from_IntelliSense_because_they_are_public_only_for_the_source_generator()
    {
        Type[] guards =
        [
            typeof(MaybeJsonGuard),
        ];

        var visible = guards.Where(static guard => guard.GetCustomAttribute<EditorBrowsableAttribute>()?.State != EditorBrowsableState.Never);

        Assert.Empty(visible);
    }

    private sealed record Customer(Maybe<string> Nickname);

    // The converter of a user who has decided what a maybe looks like in their JSON: the value, or null.
    private sealed class NullOrValueConverter : JsonConverter<Maybe<int>>
    {
        public override bool HandleNull => true;

        public override Maybe<int> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
            reader.TokenType == JsonTokenType.Null ? default : Maybe.Some(reader.GetInt32());

        public override void Write(Utf8JsonWriter writer, Maybe<int> value, JsonSerializerOptions options)
        {
            if (value.TryGetValue(out var number))
            {
                writer.WriteNumberValue(number);
            }
            else
            {
                writer.WriteNullValue();
            }
        }
    }
}
