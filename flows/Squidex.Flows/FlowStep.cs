// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

namespace Squidex.Flows;

public abstract record FlowStep
{
    public virtual ValueTask ValidateAsync(FlowValidationContext validationContext, AddStepError addError,
        CancellationToken ct)
    {
        return default;
    }

    public virtual ValueTask PrepareAsync(FlowExecutionContext executionContext,
        CancellationToken ct)
    {
        return default;
    }

    public abstract ValueTask<FlowStepResult> ExecuteAsync(FlowExecutionContext executionContext,
        CancellationToken ct);
}
