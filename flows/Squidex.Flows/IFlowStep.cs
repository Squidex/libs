// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

namespace Squidex.Flows;

public interface IFlowStep
{
    ValueTask ValidateAsync(FlowValidationContext validationContext, AddError addError,
        CancellationToken ct)
    {
        return default;
    }

    ValueTask PrepareAsync(FlowContext context, FlowExecutionContext executionContext,
        CancellationToken ct)
    {
        return default;
    }

    ValueTask<FlowStepResult> ExecuteAsync(FlowContext context, FlowExecutionContext executionContext,
        CancellationToken ct);
}
