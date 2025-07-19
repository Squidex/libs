// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace Squidex.Events.Mongo;

public sealed class MongoHeaderValueSerializer : SerializerBase<HeaderValue>
{
    public override HeaderValue Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
    {
        var reader = context.Reader;
        switch (reader.CurrentBsonType)
        {
            case BsonType.String:
                return reader.ReadString();
            case BsonType.Int32:
                return reader.ReadInt32();
            case BsonType.Int64:
                return reader.ReadInt64();
            case BsonType.Double:
                return reader.ReadDouble();
            case BsonType.Boolean:
                return reader.ReadBoolean();
            case BsonType.Null:
                reader.ReadNull();
                return default;
            default:
                throw new BsonSerializationException($"Unsupported token '{reader.CurrentBsonType}'.");
        }
    }

    public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, HeaderValue value)
    {
        var writer = context.Writer;
        switch (value.Value)
        {
            case string s:
                writer.WriteString(s);
                break;
            case double n:
                writer.WriteDouble(n);
                break;
            case bool b:
                writer.WriteBoolean(b);
                break;
            case null:
                writer.WriteNull();
                break;
            default:
                throw new BsonSerializationException($"Unsupported value type '{value.GetType()}'.");
        }
    }
}
