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

namespace Squidex.Flows.Execution.MongoDb;

public sealed class MongoFlowStateStore<TContext>(IMongoDatabase database, JsonSerializerOptions jsonSerializerOptions) : IFlowStateStore<TContext>
{
    private readonly IMongoCollection<MongoFlowStateEntity> collection = database.GetCollection<MongoFlowStateEntity>("FlowStates");

    public Task CancelByDefinitionIdAsync(string definitionId,
        CancellationToken ct = default)
    {
        return collection.UpdateManyAsync(x => x.DefinitionId == definitionId,
            Builders<MongoFlowStateEntity>.Update.Set(x => x.DueTime, null),
            cancellationToken: ct);
    }

    public Task CancelByInstanceIdAsync(Guid instanceId,
        CancellationToken ct = default)
    {
        return collection.UpdateManyAsync(x => x.Id == instanceId,
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

    public Task EnqueueAsync(Guid instanceId, Instant nextAttempt,
        CancellationToken ct = default)
    {
        return collection.UpdateManyAsync(x => x.Id == instanceId,
            Builders<MongoFlowStateEntity>.Update.Set(x => x.DueTime, nextAttempt.ToDateTimeOffset()),
            cancellationToken: ct);
    }

    public async Task<ExecutionState<TContext>?> FindAsync(Guid id,
        CancellationToken ct = default)
    {
        var entity = await collection.Find(x => x.Id == id).FirstOrDefaultAsync(ct);
        if (entity == null)
        {
            return null;
        }

        return ParseState(entity);
    }

    public async Task<(List<ExecutionState<TContext>> Items, long Total)> QueryByOwnerAsync(string ownerId, string? definitionId = null, int skip = 0, int take = 20,
        CancellationToken ct = default)
    {
        var filters = new List<FilterDefinition<MongoFlowStateEntity>>
        {
            Builders<MongoFlowStateEntity>.Filter.Eq(x => x.OwnerId, ownerId)
        };

        if (definitionId != null)
        {
            filters.Add(Builders<MongoFlowStateEntity>.Filter.Eq(x => x.DefinitionId, definitionId));
        }

        var filter = Builders<MongoFlowStateEntity>.Filter.And(filters);

        var entitiesTotal = await collection.Find(filter).CountDocumentsAsync(ct);
        var entitiesItems = await collection.Find(filter).Skip(skip).Limit(take).ToListAsync(ct);

        var items = entitiesItems.Select(ParseState).ToList();

        return (items, entitiesTotal);
    }

    public async IAsyncEnumerable<ExecutionState<TContext>> QueryPendingAsync(Instant now,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var entitiesLimit = now.ToDateTimeOffset();
        var entitiesItems = await collection.Find(x => x.DueTime < entitiesLimit).ToCursorAsync(ct);

        while (await entitiesItems.MoveNextAsync(ct))
        {
            foreach (var item in entitiesItems.Current)
            {
                yield return ParseState(item);
            }
        }
    }

    public async Task StoreAsync(List<ExecutionState<TContext>> states,
        CancellationToken ct)
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
                        DefinitionId = state.DefinitionId,
                        DueTime = state.NextRun?.ToDateTimeOffset(),
                        OwnerId = state.OwnerId,
                        State = JsonSerializer.Serialize(state, jsonSerializerOptions)
                    })
                {
                    IsUpsert = true
                });
        }

        await collection.BulkWriteAsync(batch, cancellationToken: ct);
    }

    private ExecutionState<TContext> ParseState(MongoFlowStateEntity entity)
    {
        return JsonSerializer.Deserialize<ExecutionState<TContext>>(entity.State, jsonSerializerOptions)!;
    }
}
