// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

namespace Squidex.Flows.Internal;

public sealed class FlowDefinition
{
    public Guid InitialStep { get; set; }

    public Dictionary<Guid, FlowStepDefinition> Steps { get; set; } = [];

    public IFlowStep? GetInitialStep()
    {
        return GetStep(InitialStep);
    }

    public IFlowStep? GetStep(Guid id)
    {
        if (id == Guid.Empty)
        {
            return null;
        }

        if (Steps.TryGetValue(id, out var stepDefinition))
        {
            return stepDefinition.Step;
        }

        return null;
    }
}
