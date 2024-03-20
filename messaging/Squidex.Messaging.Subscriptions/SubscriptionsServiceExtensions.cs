// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Squidex.Caching;
using Squidex.Messaging;
using Squidex.Messaging.Implementation;
using Squidex.Messaging.Subscriptions;
using Squidex.Messaging.Subscriptions.Implementation;
using Squidex.Messaging.Subscriptions.Messages;

namespace Microsoft.Extensions.DependencyInjection;

public static class SubscriptionsServiceExtensions
{
    public static IServiceCollection AddMessagingSubscriptions(this IServiceCollection services, bool consume = true, Action<ChannelOptions>? configure = null, string channelName = "subscriptions")
    {
        var channel = new ChannelName(channelName, ChannelType.Topic);

        services.AddReplicatedCache();
        services.AddMessaging(channel, consume, configure);

        services.AddSingletonAs<SubscriptionService>()
            .As<ISubscriptionService>().As<IMessageHandler>();

        services.AddSingletonAs<MessagingDataProvider>()
            .AsSelf();

        services.AddSingletonAs<MessagingDataProvider>()
            .As<IMessagingDataProvider>();

        services.TryAddSingleton<IMessagingDataStore,
            InMemoryMessagingDataStore>();

        services.Configure<MessagingOptions>(options =>
        {
            options.Routing.Add(x => x is PayloadMessageBase, channel);
        });

        return services;
    }
}
