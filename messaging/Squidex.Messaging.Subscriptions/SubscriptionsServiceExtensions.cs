// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Squidex.Messaging;
using Squidex.Messaging.Subscriptions;
using Squidex.Messaging.Subscriptions.Implementation;
using Squidex.Messaging.Subscriptions.Messages;

namespace Microsoft.Extensions.DependencyInjection;

public static class SubscriptionsServiceExtensions
{
    public static MessagingBuilder AddSubscriptions(this MessagingBuilder builder, bool consume = true, Action<ChannelOptions>? configure = null, string channelName = "subscriptions")
    {
        var channel = new ChannelName(channelName, ChannelType.Topic);

        builder.Services.AddMemoryCache();
        builder.AddChannel(channel, consume, configure);

        builder.Services.AddSingletonAs<SubscriptionService>()
            .As<ISubscriptionService>().As<IMessageHandler>();

        builder.Services.Configure<MessagingOptions>(options =>
        {
            options.Routing.Add(x => x is PayloadMessageBase, channel);
        });

        return builder;
    }
}
