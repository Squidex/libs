// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Squidex.Flows.Internal;

namespace Squidex.Flows.Steps;

#pragma warning disable MA0048 // File name must match type name

public class IfStep : IFlowStep
{
    public List<IfBranch> Branches { get; set; }

    public Guid Else { get; set; }

    public Guid? NextStep { get; set; }

    public ValueTask ValidateAsync(FlowDefinition definition, AddError addError,
        CancellationToken ct)
    {
        var branches = Branches ?? [];
        var index = 0;
        foreach (var branch in branches)
        {
            if (branch.Step == default && definition.Steps.ContainsKey(branch.Step))
            {
                var path = $"branches[{index}].step";

                addError(path, ValidationErrorType.InvalidStepId);
            }

            index++;
        }

        if (Else != default && !definition.Steps.ContainsKey(Else))
        {
            addError("else", ValidationErrorType.InvalidStepId);
        }

        return default;
    }

    public ValueTask PrepareAsync(FlowContext context, FlowExecutionContext executionContext,
        CancellationToken ct)
    {
        var nextStep = Else;

        var branches = Branches ?? [];
        foreach (var branch in branches)
        {
            if (executionContext.Evaluate(branch.Condition, context))
            {
                nextStep = branch.Step;
                break;
            }
        }

        NextStep = nextStep;
        return default;
    }

    public ValueTask<FlowStepResult> ExecuteAsync(FlowContext context, FlowExecutionContext executionContext,
        CancellationToken ct)
    {
        return new ValueTask<FlowStepResult>(FlowStepResult.Next(NextStep));
    }
}

public sealed class IfBranch
{
    public string Condition { get; set; }

    public Guid Step { get; set; }
}
