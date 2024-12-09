// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

namespace Squidex.Flows;

#pragma warning disable SA1313 // Parameter names should begin with lower-case letter

public sealed record FlowStepResult(FlowStepResultType Type, Guid? StepId)
{
    public static FlowStepResult Complete()
    {
        return new FlowStepResult(FlowStepResultType.Complete, null);
    }

    public static FlowStepResult Next(Guid? stepId = null)
    {
        return new FlowStepResult(FlowStepResultType.Next, stepId);
    }
}
