﻿// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

namespace Squidex.Flows.Steps;

public sealed record IfFlowBranch
{
    public string? Condition { get; set; }

    public Guid? NextStepId { get; set; }
}
