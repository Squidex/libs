// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

namespace Squidex.Flows.Internal;

public sealed record FlowDefinition : IEquatable<FlowDefinition>
{
    public Guid? InitialStepId { get; init; }

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

    public bool Equals(FlowDefinition? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        static bool EqualSteps(Dictionary<Guid, FlowStepDefinition>? lhs, Dictionary<Guid, FlowStepDefinition>? rhs)
        {
            if (ReferenceEquals(lhs, rhs))
            {
                return true;
            }

            if (lhs == null || rhs == null)
            {
                return false;
            }

            if (lhs.Count != rhs.Count)
            {
                return false;
            }

            return lhs.OrderBy(kv => kv.Key).SequenceEqual(rhs.OrderBy(kv => kv.Key));
        }

        return InitialStepId == other.InitialStepId && EqualSteps(Steps, other.Steps);
    }

    public override int GetHashCode()
    {
        HashCode hashCode = default;
        hashCode.Add(InitialStepId);

        if (Steps != null)
        {
            foreach (var kvp in Steps.OrderBy(kv => kv.Key))
            {
                hashCode.Add(kvp.Key);
                hashCode.Add(kvp.Value);
            }
        }

        return hashCode.ToHashCode();
    }
}
