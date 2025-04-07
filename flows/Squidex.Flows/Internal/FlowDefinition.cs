// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Generator.Equals;

namespace Squidex.Flows.Internal;

[Equatable]
public sealed partial record FlowDefinition
{
    public Guid InitialStep { get; init; }

    [UnorderedEquality]
    public Dictionary<Guid, FlowStepDefinition> Steps { get; init; } = [];

    public FlowStep? GetInitialStep()
    {
        return GetStep(InitialStep);
    }

    public FlowStep? GetStep(Guid id)
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
