// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using NodaTime;

namespace Squidex.Flows.Execution;

internal class NoRetryErrorPolicy<TContext> : IErrorPolicy<TContext>
{
    public Instant? ShouldRetry(ExecutionState<TContext> state, ExecutionStepState stepState, IFlowStep<TContext> step)
    {
        return null;
    }
}
