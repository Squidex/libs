// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

namespace Squidex.Flows.Internal;

public sealed class FlowDefinition
{
    public Guid InitialStep { get; set; }

    public Dictionary<Guid, FlowStepDefinition> Steps { get; set; } = [];
}
