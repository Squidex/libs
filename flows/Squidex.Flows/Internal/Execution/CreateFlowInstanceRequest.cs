// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

namespace Squidex.Flows.Internal.Execution;

public readonly record struct CreateFlowInstanceRequest<TContext>
{
    required public string OwnerId { get; init; }

    required public string DefinitionId { get; init; }

    required public string Description { get; init; }

    required public string ScheduleKey { get; init; }

    required public FlowDefinition Definition { get; init; }

    required public TContext Context { get; init; }
}
