// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using NodaTime;

#pragma warning disable MA0048 // File name must match type name
#pragma warning disable SA1313 // Parameter names should begin with lower-case letter

namespace Squidex.Flows.Execution;

public sealed class ExecutionStepState
{
    public ExecutionStatus Status { get; set; }

    public bool IsPrepared { get; set; }

    public List<ExecutionStepAttempt> Attempts { get; set; } = [];

    public ExecutionStepAttempt NextAttempt(Instant started)
    {
        var attempt = new ExecutionStepAttempt
        {
            Started = started
        };

        Attempts.Add(attempt);

        return attempt;
    }
}

public sealed class ExecutionStepAttempt
{
    public List<ExecutionStepLogEntry> Log { get; set; } = [];

    public Instant Started { get; set; }

    public Instant Completed { get; set; }

    public Exception? Error { get; set; }
}

public sealed record ExecutionStepLogEntry(Instant Timestamp, string Message, string? Dump);
