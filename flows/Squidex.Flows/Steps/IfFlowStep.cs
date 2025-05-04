// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System.ComponentModel.DataAnnotations;
using Squidex.Flows.Internal;

namespace Squidex.Flows.Steps;

[FlowStep(
    Title = "If",
    IconImage = "<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 -960 960 960'><path d='M600-160v-80H440v-200h-80v80H80v-240h280v80h80v-200h160v-80h280v240H600v-80h-80v320h80v-80h280v240H600zm80-80h120v-80H680v80zM160-440h120v-80H160v80zm520-200h120v-80H680v80zm0 400v-80 80zM280-440v-80 80zm400-200v-80 80z'/></svg>",
    IconColor = "#3389ff",
    Display = "Conditions",
    Description = "Create branches based on conditions.")]
[NoRetry]
public sealed record IfFlowStep : FlowStep, IEquatable<IfFlowStep>
{
    [Required]
    [Display(Name = "Branches", Description = "The delay in seconds.")]
    [Editor(FlowStepEditor.Branches)]
    public List<IfFlowBranch>? Branches { get; set; }

    [Editor(FlowStepEditor.None)]
    public Guid? ElseStepId { get; set; }

    public override ValueTask ValidateAsync(FlowValidationContext validationContext, AddStepError addError,
        CancellationToken ct)
    {
        var definition = validationContext.Definition;

        if (Branches != null)
        {
            for (var i = 0; i < Branches.Count; i++)
            {
                if (!IsValidStep(definition, Branches[i].NextStepId))
                {
                    addError($"branches[{i}].step", "Invalid Step ID.");
                }
            }
        }

        if (!IsValidStep(definition, ElseStepId))
        {
            addError("else", "Invalid Step ID");
        }

        return default;

        static bool IsValidStep(FlowDefinition definition, Guid? stepId)
        {
            return stepId == null || stepId.Value == default || definition.Steps.ContainsKey(stepId.Value);
        }
    }

    public override ValueTask<FlowStepResult> ExecuteAsync(FlowExecutionContext executionContext,
        CancellationToken ct)
    {
        return new ValueTask<FlowStepResult>(ExecuteCore(executionContext));
    }

    private FlowStepResult ExecuteCore(FlowExecutionContext executionContext)
    {
        if (Branches != null)
        {
            var index = 0;
            foreach (var branch in Branches)
            {
                if (string.IsNullOrWhiteSpace(branch.Condition))
                {
                    executionContext.Log($"Branch #{index} matched criteria without condition.");
                    return Next(branch.NextStepId ?? default);
                }

                if (executionContext.Evaluate(branch.Condition, executionContext.Context))
                {
                    executionContext.Log($"Branch #{index} matched criteria with condition '{branch.Condition}'");
                    return Next(branch.NextStepId ?? default);
                }

                index++;
            }
        }

        if (Branches == null || Branches.Count == 0)
        {
            executionContext.Log("No branch defined. Continue with 'else' branch.");
            return Next(ElseStepId ?? default);
        }

        executionContext.Log("No conditioned matched criteria. Continue with 'else' branch.");
        return Next(ElseStepId ?? default);
    }

    public bool Equals(IfFlowStep? other)
    {
        if (other is null)
        {
            return true;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        static bool EqualBranches(List<IfFlowBranch>? lhs, List<IfFlowBranch>? rhs)
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

            return lhs.SequenceEqual(rhs);
        }

        return ElseStepId == other.ElseStepId && EqualBranches(Branches, other.Branches);
    }

    public override int GetHashCode()
    {
        HashCode hashCode = default;
        hashCode.Add(ElseStepId);

        if (Branches != null)
        {
            foreach (var branch in Branches)
            {
                hashCode.Add(branch);
            }
        }

        return hashCode.ToHashCode();
    }
}
