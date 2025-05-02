// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System.ComponentModel.DataAnnotations;
using Generator.Equals;

namespace Squidex.Flows.Steps;

#pragma warning disable MA0048 // File name must match type name

[NoRetry]
[FlowStep(
    Title = "If",
    IconImage = "<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 -960 960 960'><path d='M600-160v-80H440v-200h-80v80H80v-240h280v80h80v-200h160v-80h280v240H600v-80h-80v320h80v-80h280v240H600zm80-80h120v-80H680v80zM160-440h120v-80H160v80zm520-200h120v-80H680v80zm0 400v-80 80zM280-440v-80 80zm400-200v-80 80z'/></svg>",
    IconColor = "#3389ff",
    Display = "Conditions",
    Description = "Create branches based on conditions.")]
[Equatable]
public sealed partial record IfFlowStep : FlowStep
{
    [Required]
    [Display(Name = "Branches", Description = "The delay in seconds.")]
    [Editor(FlowStepEditor.Branches)]
    [OrderedEquality]
    public List<IfBranch> Branches { get; set; }

    [Editor(FlowStepEditor.None)]
    public Guid? ElseStepId { get; set; }

    [Computed]
    public Guid NextStep { get; set; }

    public override ValueTask ValidateAsync(FlowValidationContext validationContext, AddStepError addError,
        CancellationToken ct)
    {
        var definition = validationContext.Definition;
        var branches = Branches ?? [];
        var index = 0;

        foreach (var branch in branches)
        {
            if (branch.NextStepId != null &&
                branch.NextStepId != default &&
                !definition.Steps.ContainsKey(branch.NextStepId.Value))
            {
                var path = $"branches[{index}].step";

                addError(path, "Invalid Step ID.");
            }

            index++;
        }

        if (ElseStepId != null &&
            ElseStepId != default &&
            !definition.Steps.ContainsKey(ElseStepId.Value))
        {
            addError("else", "Invalid Step ID");
        }

        return default;
    }

    public override ValueTask PrepareAsync(FlowExecutionContext executionContext,
        CancellationToken ct)
    {
        var nextStep = ElseStepId;

        var branches = Branches ?? [];
        foreach (var branch in branches)
        {
            if (executionContext.Evaluate(branch.Condition, executionContext.Context))
            {
                nextStep = branch.NextStepId;
                break;
            }
        }

        NextStep = nextStep ?? default;
        return default;
    }

    public override ValueTask<FlowStepResult> ExecuteAsync(FlowExecutionContext executionContext,
        CancellationToken ct)
    {
        return new ValueTask<FlowStepResult>(Next(NextStep));
    }
}

public sealed class IfBranch
{
    public string? Condition { get; set; }

    public Guid? NextStepId { get; set; }
}
