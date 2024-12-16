// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System.ComponentModel.DataAnnotations;

namespace Squidex.Flows.Steps;

[FlowStep(
    Title = "Script",
    IconImage = "<svg xmlns='http://www.w3.org/2000/svg' height='24' viewBox='0 -960 960 960' width='24'><path d='M300-360q-25 0-42.5-17.5T240-420v-40h60v40h60v-180h60v180q0 25-17.5 42.5T360-360h-60zm220 0q-17 0-28.5-11.5T480-400v-40h60v20h80v-40H520q-17 0-28.5-11.5T480-500v-60q0-17 11.5-28.5T520-600h120q17 0 28.5 11.5T680-560v40h-60v-20h-80v40h100q17 0 28.5 11.5T680-460v60q0 17-11.5 28.5T640-360H520z'/></svg>",
    IconColor = "#3389ff",
    Display = "Execute a script",
    Description = "Execute custom code in Javascript.")]
public class ScriptStep : IFlowStep
{
    [Script]
    [Display(Name = "Script", Description = "The script to execute.")]
    [Editor(FlowStepEditor.TextArea)]
    public string? Script { get; set; }

    public async ValueTask<FlowStepResult> ExecuteAsync(FlowContext context, FlowExecutionContext executionContext,
        CancellationToken ct)
    {
        if (!string.IsNullOrEmpty(Script))
        {
            await executionContext.RenderAsync(Script, context);
        }

        return FlowStepResult.Next();
    }
}
