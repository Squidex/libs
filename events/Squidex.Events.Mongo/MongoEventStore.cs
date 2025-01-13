// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using MongoDB.Driver.Core.Clusters;
using Squidex.Hosting;

namespace Squidex.Events.Mongo;

public partial class MongoEventStore(
    IMongoDatabase database,
    IOptions<MongoEventStoreOptions> options)
    : IEventStore, IInitializable
{
    public static readonly FilterDefinitionBuilder<MongoEventCommit> Filter =
        Builders<MongoEventCommit>.Filter;

    public static readonly ProjectionDefinitionBuilder<MongoEventCommit> Projection =
        Builders<MongoEventCommit>.Projection;

    public static readonly SortDefinitionBuilder<MongoEventCommit> Sort =
        Builders<MongoEventCommit>.Sort;

    private readonly IMongoCollection<MongoEventCommit> collection =
        database.GetCollection<MongoEventCommit>("Events2", new MongoCollectionSettings { WriteConcern = WriteConcern.WMajority });

    private readonly IMongoCollection<BsonDocument> rawCollection =
        database.GetCollection<BsonDocument>("Events2", new MongoCollectionSettings { WriteConcern = WriteConcern.WMajority });

    public IMongoCollection<BsonDocument> RawCollection => rawCollection;

    public IMongoCollection<MongoEventCommit> TypedCollection => collection;

    public bool CanUseChangeStreams { get; private set; }

    public async Task InitializeAsync(
        CancellationToken ct)
    {
        await collection.Indexes.CreateManyAsync(
        [
            new CreateIndexModel<MongoEventCommit>(
                Builders<MongoEventCommit>.IndexKeys
                    .Ascending(x => x.EventStream)
                    .Ascending(x => x.Timestamp)),
            new CreateIndexModel<MongoEventCommit>(
                Builders<MongoEventCommit>.IndexKeys
                    .Descending(x => x.Timestamp)
                    .Ascending(x => x.EventStream)),
            new CreateIndexModel<MongoEventCommit>(
                Builders<MongoEventCommit>.IndexKeys
                    .Ascending(x => x.EventStream)
                    .Descending(x => x.EventStreamOffset),
                new CreateIndexOptions
                {
                    Unique = true
                })
        ], ct);

        var clusterVersion = await database.GetMajorVersionAsync(ct);
        var clusteredAsReplica = database.Client.Cluster.Description.Type == ClusterType.ReplicaSet;

        CanUseChangeStreams = clusteredAsReplica && clusterVersion >= 4;

        BsonSerializer.TryRegisterSerializer(new HeaderValueSerializer());
    }
}
