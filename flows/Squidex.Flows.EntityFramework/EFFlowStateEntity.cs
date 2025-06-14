﻿// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

namespace Squidex.Flows.EntityFramework;

public sealed class EFFlowStateEntity
{
    public Guid Id { get; set; }

    public DateTimeOffset Created { get; set; }

    public string OwnerId { get; set; }

    public string DefinitionId { get; set; }

    public string State { get; set; }

    public int SchedulePartition { get; set; }

    public DateTimeOffset? DueTime { get; set; }
}
