// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Squidex.Assets
{
    internal sealed class MongoAssetEntity<T>
    {
        [BsonId]
        public string Key { get; set; }

        [BsonIgnoreIfDefault]
        public T Value { get; set; }

        [BsonRepresentation(BsonType.String)]
        public DateTime Expires { get; set; }
    }
}
