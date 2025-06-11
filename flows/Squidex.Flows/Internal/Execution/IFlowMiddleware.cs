// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

#pragma warning disable MA0048 // File name must match type name

namespace Squidex.Flows.Internal.Execution;

public delegate ValueTask<FlowStepResult> NextStepDelegate();

public delegate ValueTask<FlowStepResult> PipelineDelegate(FlowExecutionContext executionContext, CancellationToken ct);

public interface IFlowMiddleware
{
    ValueTask<FlowStepResult> InvokeAsync(FlowExecutionContext executionContext, NextStepDelegate next,
        CancellationToken ct);
}
