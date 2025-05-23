// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System.Runtime.CompilerServices;
using System.Text.Json;
using MongoDB.Driver;
using NodaTime;
using Squidex.Flows.Internal.Execution;
using Squidex.Hosting;

namespace Squidex.Flows.Mongo;

public sealed class MongoFlowStateStore<TContext>(IMongoDatabase database, JsonSerializerOptions jsonSerializerOptions)
    : IFlowStateStore<TContext>, IInitializable
     where TContext : FlowContext
{
    private readonly IMongoCollection<MongoFlowStateEntity> collection = database.GetCollection<MongoFlowStateEntity>("FlowStates");

    public async Task InitializeAsync(CancellationToken ct)
    {
        await collection.Indexes.CreateOneAsync(
            new CreateIndexModel<MongoFlowStateEntity>(
                Builders<MongoFlowStateEntity>.IndexKeys
                    .Ascending(x => x.DueTime)
                    .Ascending(x => x.SchedulePartition)),
            cancellationToken: ct);
    }

    public async Task<bool> CancelByInstanceIdAsync(Guid instanceId,
        CancellationToken ct = default)
    {
        var update =
            await collection.UpdateManyAsync(x => x.Id == instanceId,
                Builders<MongoFlowStateEntity>.Update.Set(x => x.DueTime, null),
                cancellationToken: ct);

        return update.ModifiedCount > 0;
    }

    public async Task<bool> EnqueueAsync(Guid instanceId, Instant nextAttempt,
        CancellationToken ct = default)
    {
        var update =
            await collection.UpdateManyAsync(x => x.Id == instanceId,
                Builders<MongoFlowStateEntity>.Update.Set(x => x.DueTime, nextAttempt.ToDateTimeOffset()),
                cancellationToken: ct);

        return update.ModifiedCount > 0;
    }

    public Task CancelByDefinitionIdAsync(string definitionId,
        CancellationToken ct = default)
    {
        return collection.UpdateManyAsync(x => x.DefinitionId == definitionId,
            Builders<MongoFlowStateEntity>.Update.Set(x => x.DueTime, null),
            cancellationToken: ct);
    }

    public Task CancelByOwnerIdAsync(string ownerId,
        CancellationToken ct = default)
    {
        return collection.UpdateManyAsync(x => x.OwnerId == ownerId,
            Builders<MongoFlowStateEntity>.Update.Set(x => x.DueTime, null),
            cancellationToken: ct);
    }

    public Task DeleteByOwnerIdAsync(string ownerId,
        CancellationToken ct = default)
    {
        return collection.DeleteManyAsync(x => x.OwnerId == ownerId, ct);
    }

    public async Task<FlowExecutionState<TContext>?> FindAsync(Guid id,
        CancellationToken ct = default)
    {
        var entity = await collection.Find(x => x.Id == id).FirstOrDefaultAsync(ct);
        if (entity == null)
        {
            return null;
        }

        return ParseState(entity);
    }

    public async Task<(List<FlowExecutionState<TContext>> Items, long Total)> QueryByOwnerAsync(string ownerId, string? definitionId = null, int skip = 0, int take = 20,
        CancellationToken ct = default)
    {
        var filters = new List<FilterDefinition<MongoFlowStateEntity>>
        {
            Builders<MongoFlowStateEntity>.Filter.Eq(x => x.OwnerId, ownerId),
        };

        if (definitionId != null)
        {
            filters.Add(Builders<MongoFlowStateEntity>.Filter.Eq(x => x.DefinitionId, definitionId));
        }

        var filter = Builders<MongoFlowStateEntity>.Filter.And(filters);

        var entitiesItems = await collection.Find(filter).Skip(skip).Limit(take).SortByDescending(x => x.Created).ToListAsync(ct);
        var entitiesTotal = (long)entitiesItems.Count;
        if (entitiesTotal >= take || skip > 0)
        {
            entitiesTotal = await collection.Find(filter).CountDocumentsAsync(ct);
        }

        var items = entitiesItems.Select(ParseState).ToList();

        return (items, entitiesTotal);
    }

    public async IAsyncEnumerable<FlowExecutionState<TContext>> QueryPendingAsync(int[] partitions, Instant now,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var queryLimit = now.ToDateTimeOffset();
        var queryItems = await collection.Find(x => x.DueTime != null && x.DueTime < queryLimit && partitions.Contains(x.SchedulePartition)).ToCursorAsync(ct);

        while (await queryItems.MoveNextAsync(ct))
        {
            foreach (var item in queryItems.Current)
            {
                yield return ParseState(item);
            }
        }
    }

    public async Task StoreAsync(List<FlowExecutionState<TContext>> states,
        CancellationToken ct = default)
    {
        if (states.Count == 0)
        {
            return;
        }

        var batch = new List<ReplaceOneModel<MongoFlowStateEntity>>();
        foreach (var state in states)
        {
            batch.Add(
                new ReplaceOneModel<MongoFlowStateEntity>(
                    Builders<MongoFlowStateEntity>.Filter.Eq(x => x.Id, state.InstanceId),
                    new MongoFlowStateEntity
                    {
                        Id = state.InstanceId,
                        Created = state.Created.ToDateTimeOffset(),
                        DefinitionId = state.DefinitionId,
                        DueTime = state.NextRun?.ToDateTimeOffset(),
                        SchedulePartition = state.SchedulePartition,
                        OwnerId = state.OwnerId,
                        State = JsonSerializer.Serialize(state, jsonSerializerOptions),
                    })
                {
                    IsUpsert = true,
                });
        }

        await collection.BulkWriteAsync(batch, cancellationToken: ct);
    }

    private FlowExecutionState<TContext> ParseState(MongoFlowStateEntity entity)
    {
        var state = JsonSerializer.Deserialize<FlowExecutionState<TContext>>(entity.State, jsonSerializerOptions)!;

        if (entity.DueTime != null)
        {
            state.NextRun = Instant.FromDateTimeOffset(entity.DueTime.Value);
        }
        else
        {
            state.NextRun = null;
        }

        return state;
    }
}
