// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using NodaTime;

namespace Squidex.Flows.Execution;

public interface IFlowStateStore<TContext> where TContext : FlowContext
{
    Task StoreAsync(List<ExecutionState<TContext>> states,
        CancellationToken ct);

    Task EnqueueAsync(Guid instanceId, Instant nextAttempt,
        CancellationToken ct = default);

    Task CancelByInstanceIdAsync(Guid instanceId,
        CancellationToken ct = default);

    Task CancelByDefinitionIdAsync(string definitionId,
        CancellationToken ct = default);

    Task CancelByOwnerIdAsync(string ownerId,
        CancellationToken ct = default);

    IAsyncEnumerable<ExecutionState<TContext>> QueryPendingAsync(Instant now,
        CancellationToken ct = default);

    Task<(List<ExecutionState<TContext>> Items, long Total)> QueryByOwnerAsync(string ownerId, string? definitionId = null, int skip = 0, int take = 20,
        CancellationToken ct = default);

    Task<ExecutionState<TContext>?> FindAsync(Guid id,
        CancellationToken ct = default);
}
