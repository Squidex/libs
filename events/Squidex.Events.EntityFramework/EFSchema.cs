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
    public static ModelBuilder AddEventStore(this ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<EFEventCommit>(b =>
        {
            // b.HasIndex(nameof(EFEventCommit.EventStream), nameof(EFEventCommit.EventStreamOffset)).IsUnique();
            // b.HasIndex(nameof(EFEventCommit.EventStream), nameof(EFEventCommit.Position));
            // b.HasIndex(nameof(EFEventCommit.EventStream), nameof(EFEventCommit.Timestamp));
        });

        modelBuilder.Entity<EFPosition>();

        return modelBuilder;
    }
}
