// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;

namespace Squidex.Events;

public static class TestHelpers
{
    public sealed class ObjectHolder<T>
    {
        [BsonRequired]
        public T Value { get; set; }
    }

    public static T SerializeAndDeserializeBson<T>(this T value)
    {
        return SerializeAndDeserializeBson<T, T>(value);
    }

    public static TOut SerializeAndDeserializeBson<TIn, TOut>(this TIn value)
    {
        using var stream = new MemoryStream();

        using (var writer = new BsonBinaryWriter(stream))
        {
            BsonSerializer.Serialize(writer, new ObjectHolder<TIn> { Value = value });
        }

        stream.Position = 0;

        using (var reader = new BsonBinaryReader(stream))
        {
            return BsonSerializer.Deserialize<ObjectHolder<TOut>>(reader).Value;
        }
    }
}
