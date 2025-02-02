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

            b.HasKey(nameof(EFMessagingDataEntity.Group), nameof(EFMessagingDataEntity.Key));
            b.HasIndex(x => x.Expiration);
        });

        return modelBuilder;
    }

    public static ModelBuilder UseMessagingTransport(this ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<EFMessage>(b =>
        {
            b.ToTable("Messages");

            b.HasIndex(nameof(EFMessage.ChannelName), nameof(EFMessage.TimeHandled));
        });

        return modelBuilder;
    }
}
