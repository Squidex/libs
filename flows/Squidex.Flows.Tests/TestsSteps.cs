// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

#pragma warning disable MA0048 // File name must match type name

using System.ComponentModel.DataAnnotations;

namespace Squidex.Flows;

[NoRetry]
public sealed class NotRetryableNoopStep : IFlowStep
{
    public ValueTask<FlowStepResult> ExecuteAsync(FlowContext context, FlowExecutionContext executionContext,
        CancellationToken ct)
    {
        return default;
    }
}

public sealed class NoopStep : IFlowStep
{
    public ValueTask<FlowStepResult> ExecuteAsync(FlowContext context, FlowExecutionContext executionContext,
        CancellationToken ct)
    {
        return default;
    }
}

public sealed class NoopStepWithRequiredProperty : IFlowStep
{
    [Required]
    public string Required { get; set; }

    public ValueTask<FlowStepResult> ExecuteAsync(FlowContext context, FlowExecutionContext executionContext,
        CancellationToken ct)
    {
        return default;
    }
}

public sealed class NoopStepWithCustomValidation : IFlowStep
{
    public ValueTask ValidateAsync(FlowValidationContext validationContext, AddStepError addError,
        CancellationToken ct)
    {
        addError("Custom", "The Custom field has validation rules.");
        return default;
    }

    public ValueTask<FlowStepResult> ExecuteAsync(FlowContext context, FlowExecutionContext executionContext,
        CancellationToken ct)
    {
        return default;
    }
}
