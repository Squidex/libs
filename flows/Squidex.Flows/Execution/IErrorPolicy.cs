// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using NodaTime;

namespace Squidex.Flows.Execution;

public interface IErrorPolicy<TContext> where TContext : FlowContext
{
    Instant? ShouldRetry(FlowExecutionState<TContext> state, ExecutionStepState stepStep, IFlowStep step);
}
