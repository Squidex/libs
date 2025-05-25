// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Squidex.Flows.Mongo;

public class MongoCronJobEntity
{
    public string Id { get; set; }

    [BsonElement("n")]
    [BsonRepresentation(BsonType.String)]
    public DateTimeOffset DueTime { get; set; }

    [BsonElement("t")]
    public string Data { get; set; }
}
