// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Squidex.Messaging;
using Squidex.Messaging.EntityFramework;

namespace Microsoft.Extensions.DependencyInjection;

public static class MessagingServiceExtensions
{
    public static MessagingBuilder AddEntityFrameworkTransport<T>(this MessagingBuilder builder, IConfiguration config, Action<EFTransportOptions>? configure = null,
        string configPath = "messaging:ef") where T : DbContext
    {
        builder.Services.Configure(config, configPath, configure);

        builder.Services.AddSingletonAs<EFTransport<T>>()
            .As<IMessagingTransport>();
        builder.Services.AddDbContextFactory<T>();
        builder.Services.AddSingleton(TimeProvider.System);

        return builder;
    }

    public static MessagingBuilder AddEntityFrameworkDataStore<T>(this MessagingBuilder builder, IConfiguration config, Action<EFMessagingDataStoreOptions>? configure = null,
        string configPath = "messaging:ef:subscriptions") where T : DbContext
    {
        builder.Services.Configure(config, configPath, configure);

        builder.Services.AddSingletonAs<EFMessagingDataStore<T>>()
            .As<IMessagingDataStore>();
        builder.Services.AddDbContextFactory<T>();
        builder.Services.AddSingleton(TimeProvider.System);

        return builder;
    }

    public static ModelBuilder AddMessagingDataStore(this ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<EFMessagingDataEntity>(b =>
        {
            b.HasKey(nameof(EFMessagingDataEntity.Group), nameof(EFMessagingDataEntity.Key));
            b.HasIndex(x => x.Expiration);
        });

        return modelBuilder;
    }

    public static ModelBuilder AddMessagingTransport(this ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<EFMessage>(b =>
        {
            b.HasIndex(nameof(EFMessage.ChannelName), nameof(EFMessage.TimeHandled));
        });

        return modelBuilder;
    }
}
