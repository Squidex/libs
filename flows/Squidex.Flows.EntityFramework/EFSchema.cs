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
    public static ModelBuilder UseCronJobs(this ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<EFCronJobEntity>(b =>
        {
            b.ToTable("CronJobs");
            b.HasIndex(x => x.DueTime);
            b.Property(x => x.Id).HasMaxLength(255);
        });

        return modelBuilder;
    }

    public static ModelBuilder UseFlows(this ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<EFFlowStateEntity>(b =>
        {
            b.ToTable("Flows");
            b.HasIndex(x => new { x.DueTime, x.SchedulePartition });
            b.Property(x => x.Id).ValueGeneratedNever();
            b.Property(x => x.DefinitionId).HasMaxLength(255);
            b.Property(x => x.OwnerId).HasMaxLength(255);
        });

        return modelBuilder;
    }
}
