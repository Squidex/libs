// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Squidex.Flows.EntityFramework;

namespace Microsoft.EntityFrameworkCore;

public static class EFSchema
{
    public static ModelBuilder UseFlows(this ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<EFFlowStateEntity>(b =>
        {
            b.HasIndex(x => new { x.DueTime, x.SchedulePartition });
        });

        return modelBuilder;
    }
}
