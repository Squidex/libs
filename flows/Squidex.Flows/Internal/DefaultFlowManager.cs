// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Squidex.Flows.Internal.Execution;

namespace Squidex.Flows.Internal;

public sealed class DefaultFlowManager<TContext>(
    IFlowStateStore<TContext> flowStateStore,
    IFlowExecutor<TContext> flowExecutor)
    : IFlowManager<TContext> where TContext : FlowContext
{
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

    public Task ValidateAsync(FlowDefinition definition, AddError addError,
        CancellationToken ct)
    {
        return flowExecutor.ValidateAsync(definition, addError, ct);
    }

    public Task SimulateAsync(FlowExecutionState<TContext> state,
        CancellationToken ct)
    {
        return flowExecutor.SimulateAsync(state, ct);
    }

    public Task CancelByInstanceIdAsync(Guid instanceId,
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

    public Task<(List<FlowExecutionState<TContext>> Items, long Total)> QueryByOwnerAsync(string ownerId, string? definitionId = null, int skip = 0, int take = 20,
        CancellationToken ct = default)
    {
        return flowStateStore.QueryByOwnerAsync(ownerId, definitionId, skip, take, ct);
    }
}
