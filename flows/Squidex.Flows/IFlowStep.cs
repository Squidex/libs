// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Squidex.Flows.Internal;

namespace Squidex.Flows;

public interface IFlowStep
{
    ValueTask ValidateAsync(FlowDefinition definition, AddError addError,
        CancellationToken ct)
    {
        return default;
    }
}

public interface IFlowStep<TContext> : IFlowStep
{
    ValueTask PrepareAsync(TContext context, FlowExecutionContext executionContext,
        CancellationToken ct)
    {
        return default;
    }

    ValueTask<FlowStepResult> ExecuteAsync(TContext context, FlowExecutionContext executionContext,
        CancellationToken ct);
}
