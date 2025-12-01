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
    private static readonly FilterDefinitionBuilder<MongoEventCommit> Filter =
        Builders<MongoEventCommit>.Filter;

    private static readonly ProjectionDefinitionBuilder<MongoEventCommit> Projection =
        Builders<MongoEventCommit>.Projection;

    private static readonly SortDefinitionBuilder<MongoEventCommit> Sort =
        Builders<MongoEventCommit>.Sort;

    private readonly IMongoCollection<MongoEventCommit> collection =
        database.GetCollection<MongoEventCommit>(
            options.Value.CollectionName,
            new MongoCollectionSettings { WriteConcern = WriteConcern.WMajority });

    private readonly IMongoCollection<BsonDocument> rawCollection =
        database.GetCollection<BsonDocument>(
            options.Value.CollectionName,
            new MongoCollectionSettings { WriteConcern = WriteConcern.WMajority });

    private QueryStrategy queryStrategy;

    public IMongoCollection<BsonDocument> RawCollection => rawCollection;

    public IMongoCollection<MongoEventCommit> TypedCollection => collection;

    public bool CanUseChangeStreams { get; private set; }

    public async Task InitializeAsync(
        CancellationToken ct)
    {
        var versionInfo = await MongoVersionInfo.DetectAsync(database, ct);

        queryStrategy = versionInfo.Dervivate == MongoDerivate.MongoDB ?
            new QueryByTimestamp() :
            new QueryByGlobalPosition(collection);
        await queryStrategy.InitializeAsync(collection, ct);

        await collection.Indexes.CreateOneAsync(
            new CreateIndexModel<MongoEventCommit>(
                Builders<MongoEventCommit>.IndexKeys
                    .Ascending(x => x.EventStream)
                    .Descending(x => x.EventStreamOffset),
                new CreateIndexOptions
                {
                    Unique = true,
                }),
            cancellationToken: ct);

        var clusterVersion = versionInfo.Major;
        var clusteredAsReplica = database.Client.Cluster.Description.Type == ClusterType.ReplicaSet;

        CanUseChangeStreams = (clusteredAsReplica && clusterVersion >= 4) || options.Value.UseChangeStreams;

        BsonSerializer.TryRegisterSerializer(new MongoHeaderValueSerializer());
    }
}
