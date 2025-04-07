// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Squidex.Flows.Internal.Execution;

namespace Squidex.Flows.Internal;

internal class FlowConsoleMiddleware : IFlowMiddleware
{
    public async ValueTask<FlowStepResult> InvokeAsync(FlowExecutionContext executionContext, NextStepDelegate next,
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
