// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

namespace Squidex.Flows;

public interface IFlowStepFactory<TContext>
{
    string StepType { get; set; }

    IFlowStep<TContext> CreateStep(FlowStepDescriptor definition);
}
