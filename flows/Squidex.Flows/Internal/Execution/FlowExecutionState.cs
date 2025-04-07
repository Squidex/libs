﻿// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using NodaTime;

namespace Squidex.Flows.Internal.Execution;

public sealed class FlowExecutionState<TContext> where TContext : FlowContext
{
    required public Guid InstanceId { get; set; }

    required public string OwnerId { get; set; }

    required public string DefinitionId { get; set; }

    required public FlowDefinition Definition { get; set; }

    required public TContext Context { get; set; }

    public string Description { get; set; } = string.Empty;

    public string ScheduleKey { get; set; } = string.Empty;

    public int SchedulePartition { get; set; }

    public Dictionary<Guid, ExecutionStepState> Steps { get; set; } = [];

    public Guid NextStepId { get; set; }

    public Instant? NextRun { get; set; }

    public Instant Created { get; set; }

    public Instant Completed { get; set; }

    public Instant Expires { get; set; }

    public ExecutionStatus Status { get; set; }

    public ExecutionStepState Step(Guid id)
    {
        if (!Steps.TryGetValue(id, out var stepState))
        {
            stepState = new ExecutionStepState();
            Steps[id] = stepState;
        }

        return stepState;
    }

    public void Failed(Instant now)
    {
        Status = ExecutionStatus.Failed;
        NextRun = null;
        NextStepId = default;
        Completed = now;
    }

    public void Complete(Instant now)
    {
        Completed = now;
        NextRun = null;
        NextStepId = default;
        Status = ExecutionStatus.Completed;
    }

    public void Next(Guid nextId, Instant scheduleAt)
    {
        NextRun = scheduleAt;
        NextStepId = nextId;
        Step(nextId).Status = ExecutionStatus.Scheduled;
    }

    public Guid GetNextStep(FlowStepDefinition currentStep, Guid nextId)
    {
        if (nextId == default)
        {
            nextId = currentStep.NextStepId;
        }

        if (!Definition.Steps.ContainsKey(nextId))
        {
            return default;
        }

        if (Step(nextId).Status != ExecutionStatus.Pending)
        {
            return default;
        }

        return nextId;
    }
}
