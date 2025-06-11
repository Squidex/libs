// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using Squidex.Events.Mongo;

namespace Squidex.Events;

public class EnvelopeHeadersTests
{
    static EnvelopeHeadersTests()
    {
        BsonSerializer.TryRegisterSerializer(new HeaderValueSerializer());
    }

    [Fact]
    public void Should_get_long()
    {
        var headers = new EnvelopeHeaders
        {
            ["long"] = 42,
        };

        var result = headers.GetLong("long");
        Assert.Equal(42, result);
    }

    [Fact]
    public void Should_get_long_from_empty_key()
    {
        var headers = new EnvelopeHeaders
        {
        };

        var result = headers.GetLong("long");
        Assert.Equal(0, result);
    }

    [Theory]
    [InlineData("9", 9)]
    [InlineData("A", 0)]
    [InlineData(" ", 0)]
    public void Should_get_long_from_string(string source, long expected)
    {
        var headers = new EnvelopeHeaders
        {
            ["long"] = source,
        };

        var result = headers.GetLong("long");
        Assert.Equal(expected, result);
    }

    [Fact]
    public void Should_get_string()
    {
        var headers = new EnvelopeHeaders
        {
            ["string"] = "Hello",
        };

        var result = headers.GetString("string");
        Assert.Equal("Hello", result);
    }

    [Fact]
    public void Should_get_string_from_empty_key()
    {
        var headers = new EnvelopeHeaders
        {
        };

        var result = headers.GetString("string");
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void Should_get_string_from_long()
    {
        var headers = new EnvelopeHeaders
        {
            ["string"] = 42,
        };

        var result = headers.GetString("string");
        Assert.Equal("42", result);
    }

    [Fact]
    public void Should_get_string_from_bool()
    {
        var headers = new EnvelopeHeaders
        {
            ["string"] = true,
        };

        var result = headers.GetString("string");
        Assert.Equal("true", result);
    }

    [Fact]
    public void Should_get_boolean()
    {
        var headers = new EnvelopeHeaders
        {
            ["bool"] = true,
        };

        var result = headers.GetBoolean("bool");
        Assert.True(result);
    }

    [Fact]
    public void Should_get_boolean_from_empty_key()
    {
        var headers = new EnvelopeHeaders
        {
        };

        var result = headers.GetBoolean("bool");
        Assert.False(result);
    }

    [Fact]
    public void Should_get_datetime()
    {
        var headers = new EnvelopeHeaders
        {
            ["date"] = "2023-12-11T10:09:08z",
        };

        var result = headers.GetDateTime("date");
        Assert.Equal(new DateTime(2023, 12, 11, 10, 9, 8, DateTimeKind.Utc), result);
    }

    [Fact]
    public void Should_get_datetime_with_millis()
    {
        var headers = new EnvelopeHeaders
        {
            ["date"] = "2023-12-11T10:09:08.765z",
        };

        var result = headers.GetDateTime("date");
        Assert.Equal(new DateTime(2023, 12, 11, 10, 9, 8, 765, DateTimeKind.Utc), result);
    }

    [Fact]
    public void Should_create_headers()
    {
        var headers = new EnvelopeHeaders();

        Assert.Empty(headers);
    }

    [Fact]
    public void Should_create_headers_as_copy()
    {
        var source = new EnvelopeHeaders
        {
            ["key1"] = 13,
        };

        var headers = new EnvelopeHeaders(source);

        CompareHeaders(headers, source);
    }

    [Fact]
    public void Should_clone_headers()
    {
        var source = new EnvelopeHeaders
        {
            ["key1"] = 13,
        };

        var headers = source.CloneHeaders();

        CompareHeaders(headers, source);
    }

    [Fact]
    public void Should_serialize_and_deserialize_to_json_string()
    {
        var source = new EnvelopeHeaders
        {
            ["key1"] = 13,
            ["key2"] = "Hello World",
            ["key3"] = true,
            ["key4"] = false,
        };

        var json = source.SerializeToJsonString();

        var deserialized = EnvelopeHeaders.DeserializeFromJson(json);

        CompareHeaders(deserialized, source);
    }

    [Fact]
    public void Should_serialize_and_deserialize_to_json_bytes()
    {
        var source = new EnvelopeHeaders
        {
            ["key1"] = 13,
            ["key2"] = "Hello World",
            ["key3"] = true,
            ["key4"] = false,
            ["key5"] = default,
        };

        var json = source.SerializeToJsonBytes();

        var deserialized = EnvelopeHeaders.DeserializeFromJson(json);

        CompareHeaders(deserialized, source);
    }

    [Fact]
    public void Should_serialize_and_deserialize_bson()
    {
        var source = new EnvelopeHeaders
        {
            ["key1"] = 13,
            ["key2"] = "Hello World",
            ["key3"] = true,
            ["key4"] = false,
            ["key5"] = default,
        };

        var deserialized = source.SerializeAndDeserializeBson();

        CompareHeaders(deserialized, source);
    }

    [Fact]
    public void Should_serialize_and_deserialize_bson_numbers()
    {
        var source = new BsonDocument
        {
            ["number1"] = 100,
            ["number2"] = 200L,
            ["number3"] = 300.5f,
            ["number4"] = 400.5d,
        };

        var expected = new EnvelopeHeaders
        {
            ["number1"] = 100,
            ["number2"] = 200,
            ["number3"] = 300.5,
            ["number4"] = 400.5,
        };

        var deserialized = source.SerializeAndDeserializeBson<BsonDocument, EnvelopeHeaders>();

        CompareHeaders(deserialized, expected);
    }

    private static void CompareHeaders(EnvelopeHeaders lhs, EnvelopeHeaders rhs)
    {
        foreach (var key in lhs.Keys.Concat(rhs.Keys).Distinct())
        {
            Assert.Equal(lhs[key].ToString(), rhs[key].ToString());
        }
    }
}
