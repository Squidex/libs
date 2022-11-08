// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using MongoDB.Bson.Serialization.Attributes;

#pragma warning disable MA0048 // File name must match type name

namespace Squidex.Messaging.Mongo;

internal sealed class MongoMessage
{
    public string Id { get; init; }

    public string? PrefetchId { get; init; }

    public string QueueName { get; init; }

    public byte[] MessageData { get; init; }

    public TransportHeaders MessageHeaders { get; init; }

    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime TimeToLive { get; init; }

    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime? TimeHandled { get; init; }

    public TransportResult ToTransportResult()
    {
        var message = new TransportMessage(MessageData, null, MessageHeaders);

        return new TransportResult(message, Id);
    }
}

internal sealed class MongoMessageId
{
    public string Id { get; init; }
}
