// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Squidex.Flows.Execution;

namespace Squidex.Flows.Internal;

internal class FlowConsoleMiddleware : IFlowMiddleware
{
    public async ValueTask<FlowStepResult> InvokeAsync(FlowContext context, FlowExecutionContext executionContext, IFlowStep step, NextStepDelegate next,
        CancellationToken ct)
    {
        FlowConsole.Output = executionContext.Log;
        try
        {
            return await next();
        }
        finally
        {
            FlowConsole.Output = null;
        }
    }
}
