// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using NodaTime;
using Squidex.Flows.Internal;
using static Google.Cloud.Translate.V3.BatchTranslateDocumentMetadata.Types;

namespace Squidex.Flows.Execution;

public sealed class ExecutionState<TContext>
{
    required public Guid InstanceId { get; set; }

    required public string OwnerId { get; set; }

    required public string DefinitionId { get; set; }

    required public FlowDefinition Definition { get; set; }

    public int ExecutionPartition { get; set; }

    public TContext Context { get; set; }

    required public string Description { get; set; }

    public Dictionary<Guid, ExecutionStepState> Steps { get; set; } = [];

    public Guid NextStep { get; set; }

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
        NextStep = default;
        Completed = now;
    }

    public void Complete(Instant now)
    {
        Completed = now;
        NextRun = null;
        NextStep = default;
        Status = ExecutionStatus.Completed;
    }

    public void Next(Guid nextId, Instant scheduleAt)
    {
        NextRun = scheduleAt;
        NextStep = nextId;
        Step(nextId).Status = ExecutionStatus.Scheduled;
    }
}
