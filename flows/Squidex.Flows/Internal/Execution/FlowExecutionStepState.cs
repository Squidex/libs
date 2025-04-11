// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using NodaTime;

#pragma warning disable MA0048 // File name must match type name
#pragma warning disable SA1313 // Parameter names should begin with lower-case letter

namespace Squidex.Flows.Internal.Execution;

public sealed class FlowExecutionStepState
{
    public FlowExecutionStatus Status { get; set; }

    public bool IsPrepared { get; set; }

    public List<FlowExecutionStepAttempt> Attempts { get; set; } = [];

    public FlowExecutionStepAttempt NextAttempt(Instant started)
    {
        var attempt = new FlowExecutionStepAttempt
        {
            Started = started,
        };

        Attempts.Add(attempt);

        return attempt;
    }
}

public sealed class FlowExecutionStepAttempt
{
    public List<FlowExecutionStepLogEntry> Log { get; set; } = [];

    public Instant Started { get; set; }

    public Instant Completed { get; set; }

    public string? Error { get; set; }
}

public sealed record FlowExecutionStepLogEntry(Instant Timestamp, string Message, string? Dump);
