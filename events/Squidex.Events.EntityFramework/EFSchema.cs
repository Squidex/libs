// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Squidex.Events.EntityFramework;

namespace Microsoft.EntityFrameworkCore;

public static class EFSchema
{
    public static ModelBuilder UseEventStore(this ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<EFEventCommit>(b =>
        {
            b.HasIndex(x => new { x.EventStream, x.EventStreamOffset }).IsUnique();
            b.HasIndex(x => new { x.EventStream, x.Position });
            b.HasIndex(x => new { x.EventStream, x.Timestamp });
            b.Property(x => x.EventStream).HasMaxLength(1000);
        });

        modelBuilder.Entity<EFPosition>();

        return modelBuilder;
    }
}
