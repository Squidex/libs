// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using NodaTime;
using Squidex.Flows.Internal.Execution;

namespace Squidex.Flows.Internal;

public sealed class DefaultFlowManager<TContext>(
    IFlowStateStore<TContext> flowStateStore,
    IFlowExecutor<TContext> flowExecutor)
    : IFlowManager<TContext> where TContext : FlowContext
{
    public IClock Clock { get; set; } = SystemClock.Instance;

    public async Task EnqueueAsync(CreateFlowInstanceRequest<TContext>[] requests,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(requests);

        if (requests.Length == 0)
        {
            return;
        }

        var states = requests.Select(flowExecutor.CreateState).ToList();

        await flowStateStore.StoreAsync(states, ct);
    }

    public async Task<FlowExecutionState<TContext>> SimulateAsync(CreateFlowInstanceRequest<TContext> request,
        CancellationToken ct)
    {
        var state = flowExecutor.CreateState(request);

        await flowExecutor.SimulateAsync(state, ct);
        return state;
    }

    public Task ValidateAsync(FlowDefinition definition, AddError addError,
        CancellationToken ct)
    {
        return flowExecutor.ValidateAsync(definition, addError, ct);
    }

    public Task ValidateAsync(FlowStep step, AddError addError,
        CancellationToken ct)
    {
        return flowExecutor.ValidateAsync(step, addError, ct);
    }

    public Task<bool> ForceAsync(Guid instanceId,
        CancellationToken ct = default)
    {
        return flowStateStore.EnqueueAsync(instanceId, Clock.GetCurrentInstant(), ct);
    }

    public Task<bool> CancelByInstanceIdAsync(Guid instanceId,
        CancellationToken ct = default)
    {
        return flowStateStore.CancelByInstanceIdAsync(instanceId, ct);
    }

    public Task CancelByDefinitionIdAsync(string definitionId,
        CancellationToken ct = default)
    {
        return flowStateStore.CancelByDefinitionIdAsync(definitionId, ct);
    }

    public Task CancelByOwnerIdAsync(string ownerId,
        CancellationToken ct = default)
    {
        return flowStateStore.CancelByOwnerIdAsync(ownerId, ct);
    }

    public Task DeleteByOwnerIdAsync(string ownerId,
        CancellationToken ct = default)
    {
        return flowStateStore.DeleteByOwnerIdAsync(ownerId, ct);
    }

    public Task<FlowExecutionState<TContext>?> FindInstanceAsync(Guid id,
        CancellationToken ct = default)
    {
        return flowStateStore.FindAsync(id, ct);
    }

    public Task<(List<FlowExecutionState<TContext>> Items, long Total)> QueryInstancesByOwnerAsync(string ownerId, string? definitionId = null, int skip = 0, int take = 20,
        CancellationToken ct = default)
    {
        return flowStateStore.QueryByOwnerAsync(ownerId, definitionId, skip, take, ct);
    }
}
