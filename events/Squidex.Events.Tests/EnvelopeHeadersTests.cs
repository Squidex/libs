// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using MongoDB.Bson.Serialization;
using Squidex.Events.Mongo;
using Xunit;

namespace Squidex.Events;

public class EnvelopeHeadersTests
{
    static EnvelopeHeadersTests()
    {
        BsonSerializer.TryRegisterSerializer(new HeaderValueSerializer());
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
            ["key1"] = 13
        };

        var headers = new EnvelopeHeaders(source);

        CompareHeaders(headers, source);
    }

    [Fact]
    public void Should_clone_headers()
    {
        var source = new EnvelopeHeaders
        {
            ["key1"] = 13
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
            ["key4"] = false
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
            ["key4"] = false
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
            ["key4"] = false
        };

        var deserialized = source.SerializeAndDeserializeBson();

        CompareHeaders(deserialized, source);
    }

    private static void CompareHeaders(EnvelopeHeaders lhs, EnvelopeHeaders rhs)
    {
        foreach (var key in lhs.Keys.Concat(rhs.Keys).Distinct())
        {
            Assert.Equal(lhs[key].ToString(), rhs[key].ToString());
        }
    }
}
