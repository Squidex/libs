// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Microsoft.Extensions.DependencyInjection.Extensions;
using Squidex.Messaging;
using Squidex.Messaging.Subscriptions;
using Squidex.Messaging.Subscriptions.Messages;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class SubscriptionsServiceExtensions
    {
        public static void AddSubscriptions(this IServiceCollection services, bool consume = true, Action<ChannelOptions>? configure = null, string channelName = "subscriptions")
        {
            var channel = new ChannelName(channelName, ChannelType.Topic);

            services.TryAddSingleton<ISubscriptionService,
                SubscriptionService>();

            services.AddMessaging(channel, consume, configure);

            services.Configure<MessagingOptions>(options =>
            {
                options.Routing.Add(x => x is SubscriptionsMessageBase, channel);
            });
        }
    }
}
