// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Squidex.Messaging.EntityFramework;

namespace Microsoft.EntityFrameworkCore;

public static class EFSchema
{
    public static ModelBuilder UseMessagingDataStore(this ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<EFMessagingDataEntity>(b =>
        {
            b.ToTable("MessagingData");
            b.HasKey(x => new { x.Group, x.Key });
            b.HasIndex(x => x.Expiration);
            b.Property(x => x.Group).HasMaxLength(255);
            b.Property(x => x.Key).HasMaxLength(255);
            b.Property(x => x.ValueType).HasMaxLength(255);
            b.Property(x => x.ValueFormat).HasMaxLength(255);
        });

        return modelBuilder;
    }

    public static ModelBuilder UseMessagingTransport(this ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<EFMessage>(b =>
        {
            b.ToTable("Messages");
            b.HasIndex(x => new { x.ChannelName, x.TimeHandled });
            b.Property(x => x.ChannelName).HasMaxLength(255);
            b.Property(x => x.MessageHeaders).HasMaxLength(2000);
            b.Property(x => x.QueueName).HasMaxLength(255);
        });

        return modelBuilder;
    }
}
