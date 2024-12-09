// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Squidex.Flows.Internal;

namespace Squidex.Flows.Execution;

public interface IFlowExecutor<TContext> where TContext : FlowContext
{
    Task<ExecutionState<TContext>> CreateInstanceAsync(
        string ownerId,
        string definitionId,
        string description,
        FlowDefinition definition,
        TContext context,
        CancellationToken ct);

    Task ValidateAsync(FlowDefinition definition, AddError addError,
        CancellationToken ct);

    Task ExecuteAsync(ExecutionState<TContext> state, ExecutionOptions options,
        CancellationToken ct);
}
