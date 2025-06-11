// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

namespace Squidex.Flows.Internal.Execution;

public interface IFlowExecutionCallback<TContext> where TContext : FlowContext
{
    Task OnUpdateAsync(FlowExecutionState<TContext> state,
        CancellationToken ct);
}
