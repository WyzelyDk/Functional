using System.Buffers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Wyzely.Functional.Internal;

namespace Wyzely.Functional.ResultTests;

// System.Text.Json refuses a result and the types of an error, on purpose: written as they are, a result loses its value and is read back as a success,
// and an error writes the values of its sensitive details in plain text. The way out is a type of your own, or a converter that you register.
public sealed class JsonGuardTests
{
    private const string Sensitive = "anna@example.com";

    private static readonly Error Sample = Error.NotFound("Order {OrderId} of {Customer} was not found.")
        .WithDetail("OrderId", 7)
        .WithDetail("Customer", Sensitive, isSensitive: true)
        .WithContext("Checkout");

    [Fact]
    internal void Serializing_a_result_with_a_value_throws_and_says_what_to_do_instead()
    {
        var exception = Assert.Throws<NotSupportedException>(() => JsonSerializer.Serialize(Result.Success(42)));

        Assert.Multiple(
            () => Assert.Contains("Result<Int32>", exception.Message),
            () => Assert.Contains("Map it to a type of your own", exception.Message),
            () => Assert.Contains("register your own JsonConverter", exception.Message),
            () => Assert.Contains("docs/result/safety-and-analyzers.md#serialization", exception.Message));
    }

    [Fact]
    internal void Serializing_a_failed_result_throws()
    {
        Assert.Throws<NotSupportedException>(() => JsonSerializer.Serialize(Result.Failure<int>(Sample)));
    }

    [Fact]
    internal void Serializing_a_result_without_a_value_throws()
    {
        var exception = Assert.Throws<NotSupportedException>(() => JsonSerializer.Serialize(Result.Success()));

        Assert.Contains("Result ", exception.Message);
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("null")]
    [InlineData("42")]
    internal void Deserializing_a_result_throws_for_whatever_the_json_is(string json)
    {
        Assert.Multiple(
            () => Assert.Throws<NotSupportedException>(() => JsonSerializer.Deserialize<Result<int>>(json)),
            () => Assert.Throws<NotSupportedException>(() => JsonSerializer.Deserialize<Result>(json)));
    }

    [Fact]
    internal void A_type_that_holds_a_result_cannot_be_serialized_or_deserialized_either()
    {
        var response = new Response(Result.Success(1));

        Assert.Multiple(
            () => Assert.Throws<NotSupportedException>(() => JsonSerializer.Serialize(response)),
            () => Assert.Throws<NotSupportedException>(() => JsonSerializer.Deserialize<Response>("{\"Outcome\":{}}")));
    }

    [Fact]
    internal void Serializing_an_error_throws_before_a_sensitive_value_is_written()
    {
        var buffer = new ArrayBufferWriter<byte>();
        using var writer = new Utf8JsonWriter(buffer);

        var exception = Assert.Throws<NotSupportedException>(() => JsonSerializer.Serialize(writer, Sample));

        writer.Flush();
        Assert.Multiple(
            () => Assert.DoesNotContain(Sensitive, exception.Message),
            () => Assert.DoesNotContain(Sensitive, Encoding.UTF8.GetString(buffer.WrittenSpan)),
            () => Assert.Contains("sensitive", exception.Message));
    }

    [Fact]
    internal void Serializing_the_parts_of_an_error_throws_too()
    {
        var detail = Sample.Details.Single(static candidate => candidate.IsSensitive);
        var frame = Sample.Context.Single();

        Assert.Multiple(
            () => Assert.Throws<NotSupportedException>(() => JsonSerializer.Serialize(detail)),
            () => Assert.Throws<NotSupportedException>(() => JsonSerializer.Serialize(frame)),
            () => Assert.Throws<NotSupportedException>(() => JsonSerializer.Serialize(Sample.Kind)));
    }

    [Fact]
    internal void Deserializing_an_error_and_its_parts_throws()
    {
        Assert.Multiple(
            () => Assert.Throws<NotSupportedException>(() => JsonSerializer.Deserialize<Error>("{}")),
            () => Assert.Throws<NotSupportedException>(() => JsonSerializer.Deserialize<ErrorFrame>("{}")),
            () => Assert.Throws<NotSupportedException>(() => JsonSerializer.Deserialize<ErrorDetail>("{}")),
            () => Assert.Throws<NotSupportedException>(() => JsonSerializer.Deserialize<ErrorDetail>("null")),
            () => Assert.Throws<NotSupportedException>(() => JsonSerializer.Deserialize<ErrorKind>("{}")),
            () => Assert.Throws<NotSupportedException>(() => JsonSerializer.Deserialize<ErrorKind>("null")));
    }

    [Fact]
    internal void A_null_reference_to_an_error_is_still_written_as_null_and_read_as_null()
    {
        var written = JsonSerializer.Serialize(new Failure(null));
        var read = JsonSerializer.Deserialize<Failure>("{\"Error\":null}");

        Assert.Multiple(
            () => Assert.Equal("{\"Error\":null}", written),
            () => Assert.Null(read!.Error));
    }

    [Fact]
    internal void A_converter_in_the_options_takes_the_place_of_the_guard()
    {
        var options = new JsonSerializerOptions { Converters = { new SucceededConverter() } };

        var written = JsonSerializer.Serialize(Result.Failure<int>(Sample), options);
        var read = JsonSerializer.Deserialize<Result<int>>("true", options);

        Assert.Multiple(
            () => Assert.Equal("false", written),
            () => Assert.True(read.IsSuccess));
    }

    [Fact]
    internal void The_guard_of_a_generic_type_converts_only_that_generic_type()
    {
        var guard = new ResultOfTJsonGuard();

        Assert.Multiple(
            () => Assert.True(guard.CanConvert(typeof(Result<int>))),
            () => Assert.False(guard.CanConvert(typeof(int))),
            () => Assert.False(guard.CanConvert(typeof(List<int>))));
    }

    [Fact]
    internal void The_guard_of_a_generic_type_still_refuses_with_its_message_when_the_converter_cannot_be_created()
    {
        var guard = new ResultOfTJsonGuard();

        var exception = Assert.Throws<NotSupportedException>(() => guard.CreateConverter(typeof(int*), new JsonSerializerOptions()));

        Assert.Contains("is not meant to be serialized", exception.Message);
    }

    private sealed record Response(Result<int> Outcome);

    private sealed record Failure(Error? Error);

    // The converter of a user who has decided what a result looks like in their JSON: only whether it succeeded.
    private sealed class SucceededConverter : JsonConverter<Result<int>>
    {
        public override Result<int> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
            reader.GetBoolean() ? Result.Success(1) : Result.Failure<int>("It failed.");

        public override void Write(Utf8JsonWriter writer, Result<int> value, JsonSerializerOptions options) => writer.WriteBooleanValue(value.IsSuccess);
    }
}
