// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System.ComponentModel.DataAnnotations;
using NodaTime;

namespace Squidex.Flows.Steps;

[FlowStep(
    Title = "Delay",
    IconImage = "<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 24 24'><path d='M12.516 6.984v5.25l4.5 2.672-.75 1.266-5.25-3.188v-6h1.5zM12 20.016q3.281 0 5.648-2.367t2.367-5.648-2.367-5.648T12 3.986 6.352 6.353t-2.367 5.648 2.367 5.648T12 20.016zm0-18q4.125 0 7.055 2.93t2.93 7.055-2.93 7.055T12 21.986t-7.055-2.93-2.93-7.055 2.93-7.055T12 2.016z'/></svg>",
    IconColor = "#3389ff",
    Display = "Delay workflow",
    Description = "Wait a little bit until the next step is executed.")]
[NoRetry]
public sealed record DelayFlowStep : FlowStep
{
    private static readonly int MaxDelay = (int)TimeSpan.FromDays(1).TotalSeconds;
    private IClock clock = SystemClock.Instance;

    [Required]
    [Display(Name = "Delay", Description = "The delay in seconds.")]
    [Editor(FlowStepEditor.Number)]
    public int DelayInSec { get; set; }

    public DelayFlowStep SetClock(IClock clock)
    {
        this.clock = clock;
        return this;
    }

    public override ValueTask<FlowStepResult> ExecuteAsync(FlowExecutionContext executionContext,
        CancellationToken ct)
    {
        return new ValueTask<FlowStepResult>(ExecuteCore(executionContext));
    }

    private FlowStepResult ExecuteCore(FlowExecutionContext executionContext)
    {
        if (DelayInSec < 0)
        {
            executionContext.Log("Delay was defined. Continue with next step immediately");
            return Next();
        }

        if (DelayInSec > MaxDelay)
        {
            executionContext.Log("Delay is larger than one day. Continue with next step immediately");
            return Next();
        }

        var scheduledIn = Duration.FromSeconds(DelayInSec);
        var scheduledAt = clock.GetCurrentInstant().Plus(scheduledIn);

        executionContext.Log($"Executing next step in {scheduledIn}, at {scheduledAt}");

        return NextDelayed(scheduledAt);
    }
}
