// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using NodaTime;

namespace Squidex.Flows.Internal.Execution;

public interface IFlowErrorPolicy<TContext> where TContext : FlowContext
{
    Instant? ShouldRetry(FlowExecutionState<TContext> state, FlowExecutionStepState stepState, FlowStep step, Instant now);
}
