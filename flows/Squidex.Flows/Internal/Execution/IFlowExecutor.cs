// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Squidex.Flows.Internal;

namespace Squidex.Flows.Internal.Execution;

public interface IFlowExecutor<TContext> where TContext : FlowContext
{
    FlowExecutionState<TContext> CreateState(CreateFlowInstanceRequest<TContext> request);

    Task ValidateAsync(FlowDefinition definition, AddError addError,
        CancellationToken ct);

    Task SimulateAsync(FlowExecutionState<TContext> state,
        CancellationToken ct);

    Task ExecuteAsync(FlowExecutionState<TContext> state, ExecutionOptions options,
        CancellationToken ct);
}
