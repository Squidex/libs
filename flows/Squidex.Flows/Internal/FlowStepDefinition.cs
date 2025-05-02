// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

namespace Squidex.Flows.Internal;

public sealed record FlowStepDefinition
{
    public Guid? NextStepId { get; set; }

    public string? Name { get; set; }

    public bool IgnoreError { get; set; }

    public FlowStep Step { get; set; }
}
