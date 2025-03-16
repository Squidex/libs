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
    Task StoreAsync(List<FlowExecutionState<TContext>> states,
        CancellationToken ct = default);

    Task EnqueueAsync(Guid instanceId, Instant nextAttempt,
        CancellationToken ct = default);

    Task CancelByInstanceIdAsync(Guid instanceId,
        CancellationToken ct = default);

    Task CancelByDefinitionIdAsync(string definitionId,
        CancellationToken ct = default);

    Task CancelByOwnerIdAsync(string ownerId,
        CancellationToken ct = default);

    Task DeleteByOwnerIdAsync(string ownerId,
        CancellationToken ct = default);

    IAsyncEnumerable<FlowExecutionState<TContext>> QueryPendingAsync(Instant now,
        CancellationToken ct = default);

    Task<(List<FlowExecutionState<TContext>> Items, long Total)> QueryByOwnerAsync(string ownerId, string? definitionId = null, int skip = 0, int take = 20,
        CancellationToken ct = default);

    Task<FlowExecutionState<TContext>?> FindAsync(Guid id,
        CancellationToken ct = default);
}
