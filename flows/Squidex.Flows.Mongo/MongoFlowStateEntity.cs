// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using MongoDB.Bson.Serialization.Attributes;

namespace Squidex.Flows.Mongo;

public sealed class MongoFlowStateEntity
{
    public Guid Id { get; set; }

    [BsonElement("o")]
    public string OwnerId { get; set; }

    [BsonElement("d")]
    public string DefinitionId { get; set; }

    [BsonElement("s")]
    public string State { get; set; }

    [BsonElement("p")]
    public int SchedulePartition { get; set; }

    [BsonElement("ts")]
    public DateTimeOffset? DueTime { get; set; }
}
