// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using NodaTime;

namespace Squidex.Flows.Execution;

internal class NoRetryErrorPolicy<TContext> : IErrorPolicy<TContext> where TContext : FlowContext
{
    public Instant? ShouldRetry(FlowExecutionState<TContext> state, ExecutionStepState stepState, IFlowStep step)
    {
        return null;
    }
}
