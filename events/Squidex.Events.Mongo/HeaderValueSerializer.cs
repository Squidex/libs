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

public sealed class HeaderValueSerializer : SerializerBase<HeaderValue>
{
    public override HeaderValue Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
    {
        var reader = context.Reader;
        switch (reader.CurrentBsonType)
        {
            case BsonType.String:
                return new HeaderStringValue(reader.ReadString());
            case BsonType.Int32:
                return new HeaderNumberValue(reader.ReadInt32());
            case BsonType.Int64:
                return new HeaderNumberValue(reader.ReadInt64());
            case BsonType.Boolean:
                return new HeaderBooleanValue(reader.ReadBoolean());
            default:
                throw new BsonSerializationException($"Unsupported token '{reader.CurrentBsonType}'.");
        }
    }

    public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, HeaderValue value)
    {
        var writer = context.Writer;
        switch (value)
        {
            case HeaderStringValue s:
                writer.WriteString(s.Value);
                break;
            case HeaderNumberValue n:
                writer.WriteInt64(n.Value);
                break;
            case HeaderBooleanValue b:
                writer.WriteBoolean(b.Value);
                break;
            default:
                throw new BsonSerializationException($"Unsupported value type '{value.GetType()}'.");
        }
    }
}
