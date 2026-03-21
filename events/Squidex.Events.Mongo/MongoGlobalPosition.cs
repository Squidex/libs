// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Squidex.Events.Mongo;

internal sealed class MongoGlobalPosition
{
    public long Id { get; set; }

    public long Position { get; set; }

    public DateTime? LockTaken { get; set; }

    [BsonRepresentation(BsonType.Binary)]
    [BsonGuidRepresentation(GuidRepresentation.CSharpLegacy)]
    public Guid LockOwner { get; set; }
}
