// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Squidex.Flows.Mongo;

public sealed class MongoFlowStateEntity
{
    [BsonRepresentation(BsonType.String)]
    public Guid Id { get; set; }

    [BsonElement("o")]
    public string OwnerId { get; set; }

    [BsonElement("c")]
    [BsonRepresentation(BsonType.String)]
    public DateTimeOffset Created { get; set; }

    [BsonElement("d")]
    public string DefinitionId { get; set; }

    [BsonElement("s")]
    public string State { get; set; }

    [BsonElement("p")]
    public int SchedulePartition { get; set; }

    [BsonElement("ts")]
    [BsonRepresentation(BsonType.String)]
    public DateTimeOffset? DueTime { get; set; }
}
