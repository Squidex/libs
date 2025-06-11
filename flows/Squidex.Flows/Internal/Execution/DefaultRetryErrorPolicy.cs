// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System.Reflection;
using NodaTime;

namespace Squidex.Flows.Internal.Execution;

public sealed class DefaultRetryErrorPolicy<TContext> : IFlowErrorPolicy<TContext> where TContext : FlowContext
{
    public Instant? ShouldRetry(FlowExecutionState<TContext> state, FlowExecutionStepState stepState, FlowStep step, Instant now)
    {
        if (step.GetType().GetCustomAttribute<NoRetryAttribute>() != null)
        {
            return null;
        }

        switch (stepState.Attempts.Count)
        {
            case 1:
                return now.Plus(Duration.FromMinutes(5));
            case 2:
                return now.Plus(Duration.FromHours(1));
            case 3:
                return now.Plus(Duration.FromHours(6));
            case 4:
                return now.Plus(Duration.FromHours(12));
            default:
                return null;
        }
    }
}
