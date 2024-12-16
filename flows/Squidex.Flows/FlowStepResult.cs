// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using NodaTime;

namespace Squidex.Flows;

#pragma warning disable SA1313 // Parameter names should begin with lower-case letter

public sealed record FlowStepResult(FlowStepResultType Type, Guid StepId = default, Instant Scheduled = default)
{
    public static FlowStepResult Complete()
    {
        return new FlowStepResult(FlowStepResultType.Complete);
    }

    public static FlowStepResult Next(Guid stepId = default, Instant scheduled = default)
    {
        return new FlowStepResult(FlowStepResultType.Next, stepId, scheduled);
    }
}
