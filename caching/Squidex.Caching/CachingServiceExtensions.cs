// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Microsoft.Extensions.DependencyInjection.Extensions;
using Squidex.Caching;
using Squidex.Messaging;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class CachingServiceExtensions
    {
        public static void AddBackgroundCache(this IServiceCollection services)
        {
            services.TryAddSingleton<IBackgroundCache, BackgroundCache>();
        }

        public static void AddAsyncLocalCache(this IServiceCollection services)
        {
            services.TryAddSingleton<ILocalCache, AsyncLocalCache>();
        }

        public static void AddReplicatedCache(this IServiceCollection services, Action<ReplicatedCacheOptions>? configureOptions = null)
        {
            if (configureOptions != null)
            {
                services.Configure(configureOptions);
            }
            else
            {
                services.AddOptions();
            }

            services.AddSingleton<ReplicatedCache>();

            services.TryAddSingleton<IReplicatedCache>(
                c => c.GetRequiredService<ReplicatedCache>());

            services.TryAddSingleton<IMessageHandler<CacheInvalidateMessage>>(
                c => c.GetRequiredService<ReplicatedCache>());
        }

        public static void AddReplicatedCacheMessaging(this IServiceCollection services, bool consume = true, Action<ChannelOptions>? configure = null, string channelName = "caching")
        {
            var channel = new ChannelName(channelName, ChannelType.Topic);

            services.AddMessaging(channel, consume, configure);

            services.Configure<MessagingOptions>(options =>
            {
                options.Routing.Add(x => x is CacheInvalidateMessage, channel);
            });
        }
    }
}
