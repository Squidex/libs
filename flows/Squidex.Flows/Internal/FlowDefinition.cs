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
    public Guid? InitialStepId { get; init; }

    [UnorderedEquality]
    public Dictionary<Guid, FlowStepDefinition> Steps { get; init; } = [];

    public FlowStep? GetInitialStepId()
    {
        return GetStep(InitialStepId);
    }

    public FlowStep? GetStep(Guid? id)
    {
        if (id == null || id == Guid.Empty)
        {
            return null;
        }

        if (Steps.TryGetValue(id.Value, out var stepDefinition))
        {
            return stepDefinition.Step;
        }

        return null;
    }
}
