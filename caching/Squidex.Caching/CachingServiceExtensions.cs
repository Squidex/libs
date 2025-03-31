// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Microsoft.Extensions.DependencyInjection.Extensions;
using Squidex.Caching;
using Squidex.Messaging;

namespace Microsoft.Extensions.DependencyInjection;

public static class CachingServiceExtensions
{
    public static IServiceCollection AddBackgroundCache(this IServiceCollection services)
    {
        services.TryAddSingleton<IBackgroundCache, BackgroundCache>();

        return services;
    }

    public static IServiceCollection AddAsyncLocalCache(this IServiceCollection services)
    {
        services.TryAddSingleton<ILocalCache, AsyncLocalCache>();

        return services;
    }

    public static IServiceCollection AddReplicatedCache(this IServiceCollection services)
    {
        services.AddMemoryCache();

        services.AddSingletonAs<ReplicatedCache>()
            .As<IReplicatedCache>().As<IMessageHandler<CacheInvalidateMessage>>();

        return services;
    }

    public static MessagingBuilder AddReplicatedCache(this MessagingBuilder builder, bool consume = true, Action<ChannelOptions>? configure = null, string channelName = "caching")
    {
        var channel = new ChannelName(channelName, ChannelType.Topic);

        builder.AddChannel(channel, consume, configure);
        builder.Services.AddReplicatedCache();
        builder.Services.Configure<MessagingOptions>(options =>
        {
            options.Routing.Add(x => x is CacheInvalidateMessage, channel);
        });

        return builder;
    }
}
