﻿// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Microsoft.Extensions.DependencyInjection.Extensions;
using Squidex.Hosting;
using Squidex.Messaging;
using Squidex.Messaging.Implementation;
using Squidex.Messaging.Implementation.InMemory;
using Squidex.Messaging.Implementation.Null;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class MessagingServiceExtensions
    {
        public static IServiceCollection AddMessaging(this IServiceCollection services, Action<MessagingOptions>? configure = null)
        {
            services.Configure<MessagingOptions>(options =>
            {
                configure?.Invoke(options);
            });

            services.TryAddSingleton<ITransportSerializer,
                NewtonsoftJsonTransportSerializer>();

            services.TryAddSingleton<IBackgroundProcess,
                SubscriptionCleaner>();

            services.TryAddSingleton<IMessageBus,
                DefaultMessageBus>();

            services.TryAddSingleton<ISubscriptionManager,
                DefaultSubscriptionManager>();

            services.TryAddSingleton<ISubscriptionStore,
                InMemorySubscriptionStore>();

            services.TryAddSingleton<ITransport,
                NullTransport>();

            services.TryAddSingleton<IInstanceNameProvider,
                HostNameInstanceNameProvider>();

            services.TryAddSingleton<IClock,
                DefaultClock>();

            services.TryAddSingleton<
                HandlerPipeline>();

            services.AddSingleton<IInternalMessageProducer,
                DelegatingProducer>();

            return services;
        }

        public static IServiceCollection AddMessaging(this IServiceCollection services, ChannelName channel, bool consume, Action<ChannelOptions>? configure = null)
        {
            services.Configure<ChannelOptions>(channel.ToString(), options =>
            {
                configure?.Invoke(options);
            });

            DelegatingConsumer FindConsumer(IServiceProvider sp)
            {
                return sp.GetRequiredService<IEnumerable<DelegatingConsumer>>().Single(x => x.Channel == channel);
            }

            AddMessaging(services);

            services.AddSingleton(
                sp => ActivatorUtilities.CreateInstance<DelegatingProducer>(sp, channel));

            if (consume)
            {
                services.AddSingleton(
                    sp => ActivatorUtilities.CreateInstance<DelegatingConsumer>(sp, channel));

                services.AddSingleton<IBackgroundProcess>(
                    FindConsumer);
            }

            return services;
        }
    }
}
