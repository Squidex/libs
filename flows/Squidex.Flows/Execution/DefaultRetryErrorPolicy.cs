// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System.Reflection;
using NodaTime;

namespace Squidex.Flows.Execution;

public sealed class DefaultRetryErrorPolicy<TContext> : IErrorPolicy<TContext> where TContext : FlowContext
{
    public Instant? ShouldRetry(FlowExecutionState<TContext> state, ExecutionStepState stepState, IFlowStep step)
    {
        if (step.GetType().GetCustomAttribute<RetryAttribute>() == null)
        {
            return null;
        }

        switch (stepState.Attempts.Count)
        {
            case 1:
                return state.Created.Plus(Duration.FromMinutes(5));
            case 2:
                return state.Created.Plus(Duration.FromHours(1));
            case 3:
                return state.Created.Plus(Duration.FromHours(6));
            case 4:
                return state.Created.Plus(Duration.FromHours(12));
            default:
                return null;
        }
    }
}
