// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System.Runtime.CompilerServices;
using System.Text.Json;
using EFCore.BulkExtensions;
using Microsoft.EntityFrameworkCore;
using NodaTime;
using Squidex.Flows.Execution;

namespace Squidex.Flows.EntityFramework;

public sealed class EFFlowStateStore<TDbContext, TContext>(
    IDbContextFactory<TDbContext> dbContextFactory,
    JsonSerializerOptions jsonSerializerOptions)
    : IFlowStateStore<TContext>
    where TContext : FlowContext
    where TDbContext : DbContext
{
    public async Task CancelByDefinitionIdAsync(string definitionId,
        CancellationToken ct = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(ct);

        await dbContext.Set<EFFlowStateEntity>()
            .Where(x => x.DefinitionId == definitionId)
            .ExecuteUpdateAsync(b => b
                .SetProperty(x => x.DueTime, (DateTimeOffset?)null),
                ct);
    }

    public async Task CancelByInstanceIdAsync(Guid instanceId,
        CancellationToken ct = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(ct);

        await dbContext.Set<EFFlowStateEntity>()
            .Where(x => x.Id == instanceId)
            .ExecuteUpdateAsync(b => b
                .SetProperty(x => x.DueTime, (DateTimeOffset?)null),
                ct);
    }

    public async Task CancelByOwnerIdAsync(string ownerId,
        CancellationToken ct = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(ct);

        await dbContext.Set<EFFlowStateEntity>()
            .Where(x => x.OwnerId == ownerId)
            .ExecuteUpdateAsync(b => b
                .SetProperty(x => x.DueTime, (DateTimeOffset?)null),
                ct);
    }

    public async Task EnqueueAsync(Guid instanceId, Instant nextAttempt,
        CancellationToken ct = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(ct);

        await dbContext.Set<EFFlowStateEntity>()
            .Where(x => x.Id == instanceId)
            .ExecuteUpdateAsync(b => b
                .SetProperty(x => x.DueTime, nextAttempt.ToDateTimeOffset()),
                ct);
    }

    public async Task DeleteByOwnerIdAsync(string ownerId,
        CancellationToken ct = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(ct);

        await dbContext.Set<EFFlowStateEntity>()
            .Where(x => x.OwnerId == ownerId)
            .ExecuteDeleteAsync(ct);
    }

    public async Task<FlowExecutionState<TContext>?> FindAsync(Guid id,
        CancellationToken ct = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(ct);

        var entity = await dbContext.Set<EFFlowStateEntity>().Where(x => x.Id == id).FirstOrDefaultAsync(ct);
        if (entity == null)
        {
            return null;
        }

        return ParseState(entity);
    }

    public async Task<(List<FlowExecutionState<TContext>> Items, long Total)> QueryByOwnerAsync(string ownerId, string? definitionId = null, int skip = 0, int take = 20,
        CancellationToken ct = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(ct);

        var query = dbContext.Set<EFFlowStateEntity>().Where(x => x.OwnerId == ownerId);

        if (definitionId != null)
        {
            query = query.Where(x => x.DefinitionId == definitionId);
        }

        var entitiesItems = await query.Skip(skip).Take(take).ToListAsync(ct);
        var entitiesTotal = (long)entitiesItems.Count;
        if (entitiesTotal >= take || skip > 0)
        {
            entitiesTotal = await query.LongCountAsync(ct);
        }

        var items = entitiesItems.Select(ParseState).ToList();

        return (items, entitiesTotal);
    }

    public async IAsyncEnumerable<FlowExecutionState<TContext>> QueryPendingAsync(Instant now,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(ct);

        var queryLimit = now.ToDateTimeOffset();
        var queryItems = dbContext.Set<EFFlowStateEntity>().Where(x => x.DueTime != null && x.DueTime < queryLimit).AsAsyncEnumerable();

        await foreach (var item in queryItems.WithCancellation(ct))
        {
            yield return ParseState(item);
        }
    }

    public async Task StoreAsync(List<FlowExecutionState<TContext>> states,
        CancellationToken ct = default)
    {
        if (states.Count == 0)
        {
            return;
        }

        await using var dbContext = await dbContextFactory.CreateDbContextAsync(ct);

        var entities =
            states.Select(x =>
                new EFFlowStateEntity
                {
                    Id = x.InstanceId,
                    DefinitionId = x.DefinitionId,
                    DueTime = x.NextRun?.ToDateTimeOffset(),
                    OwnerId = x.OwnerId,
                    State = JsonSerializer.Serialize(x, jsonSerializerOptions),
                });

        await dbContext.BulkInsertOrUpdateAsync(entities, cancellationToken: ct);
    }

    private FlowExecutionState<TContext> ParseState(EFFlowStateEntity entity)
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
