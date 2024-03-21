// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Squidex.Hosting;
using Squidex.Messaging;
using Squidex.Messaging.Implementation;
using Squidex.Messaging.Implementation.InMemory;
using Squidex.Messaging.Implementation.Null;

namespace Microsoft.Extensions.DependencyInjection;

public static class MessagingServiceExtensions
{
    public static IServiceCollection AddMessaging(this IServiceCollection services, Action<MessagingOptions>? configure = null)
    {
        services.ConfigureOptional(configure);

        services.TryAddSingleton<IMessagingSerializer,
            NewtonsoftJsonMessagingSerializer>();

        services.TryAddSingleton<IMessageBus,
            DefaultMessageBus>();

        services.TryAddSingleton<IMessagingTransport,
            NullTransport>();

        services.TryAddSingleton<IInstanceNameProvider,
            HostNameInstanceNameProvider>();

        services.TryAddSingleton(
            TimeProvider.System);

        services.TryAddSingleton<
            HandlerPipeline>();

        services.AddSingleton<IInternalMessageProducer,
            DelegatingProducer>();

        services.AddSingletonAs<MessagingDataProvider>()
            .AsSelf();

        services.AddSingletonAs<IMessagingDataProvider>(c =>
            {
                var inner = c.GetRequiredService<MessagingDataProvider>();

                if (c.GetRequiredService<IOptions<MessagingOptions>>().Value.DataCacheDuration > TimeSpan.Zero)
                {
                    return ActivatorUtilities.CreateInstance<CachingMessagingDataProvider>(c, inner);
                }

                return inner;
            })
            .As<IMessagingDataProvider>();

        services.TryAddSingleton<IMessagingDataStore,
            InMemoryMessagingDataStore>();

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
