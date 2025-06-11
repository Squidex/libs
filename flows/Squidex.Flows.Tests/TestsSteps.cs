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
public sealed record NotRetryableNoopStep : FlowStep
{
    public override ValueTask<FlowStepResult> ExecuteAsync(FlowExecutionContext executionContext,
        CancellationToken ct)
    {
        return new ValueTask<FlowStepResult>(FlowStepResult.Next());
    }
}

public sealed record NoopStep : FlowStep
{
    public override ValueTask<FlowStepResult> ExecuteAsync(FlowExecutionContext executionContext,
        CancellationToken ct)
    {
        return new ValueTask<FlowStepResult>(FlowStepResult.Next());
    }
}

public sealed record NoopStepWithRequiredProperty : FlowStep
{
    [Required]
    public string Required { get; set; }

    public override ValueTask<FlowStepResult> ExecuteAsync(FlowExecutionContext executionContext,
        CancellationToken ct)
    {
        return new ValueTask<FlowStepResult>(FlowStepResult.Next());
    }
}

public sealed record NoopStepWithCustomValidation : FlowStep
{
    public override ValueTask ValidateAsync(FlowValidationContext validationContext, AddStepError addError,
        CancellationToken ct)
    {
        addError("Custom", "The Custom field has validation rules.");
        return default;
    }

    public override ValueTask<FlowStepResult> ExecuteAsync(FlowExecutionContext executionContext,
        CancellationToken ct)
    {
        return new ValueTask<FlowStepResult>(FlowStepResult.Next());
    }
}

public sealed record NoopStepWithExpression : FlowStep
{
    [Expression]
    public string? Property { get; set; }

    public override ValueTask<FlowStepResult> ExecuteAsync(FlowExecutionContext executionContext,
        CancellationToken ct)
    {
        return new ValueTask<FlowStepResult>(FlowStepResult.Next());
    }
}
