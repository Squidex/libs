// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

namespace Squidex.Flows.Internal.Execution;

#pragma warning disable MA0048 // File name must match type name
public delegate ValueTask<FlowStepResult> NextStepDelegate();

public delegate ValueTask NextDelegate();
#pragma warning restore MA0048 // File name must match type name

public interface IFlowMiddleware
{
    ValueTask<FlowStepResult> InvokeAsync(FlowContext context, FlowExecutionContext executionContext, IFlowStep step, NextStepDelegate next,
        CancellationToken ct)
    {
        return next();
    }

    ValueTask InvokeAsync(FlowContext context, FlowExecutionContext executionContext, NextDelegate next,
        CancellationToken ct)
    {
        return next();
    }
}
