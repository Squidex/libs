// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System.ComponentModel.DataAnnotations;

namespace Squidex.Flows.Steps;

public class ScriptStep : IFlowStep
{
    [Script]
    [Display(Name = "Script", Description = "The script to execute.")]
    [Editor(FlowStepEditor.TextArea)]
    public string? Script { get; set; }

    public ValueTask<FlowStepResult> ExecuteAsync(FlowContext context, FlowExecutionContext executionContext,
        CancellationToken ct)
    {
        if (!string.IsNullOrEmpty(Script))
        {
            executionContext.Execute(Script, context);
        }

        return new ValueTask<FlowStepResult>(FlowStepResult.Next());
    }
}
