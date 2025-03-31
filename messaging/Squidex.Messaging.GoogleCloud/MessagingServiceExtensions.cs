// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Microsoft.Extensions.Configuration;
using Squidex.Messaging;
using Squidex.Messaging.GoogleCloud;

namespace Microsoft.Extensions.DependencyInjection;

public static class MessagingServiceExtensions
{
    public static MessagingBuilder AddGooglePubSubTransport(this MessagingBuilder builder, IConfiguration config, Action<GooglePubSubTransportOptions>? configure = null,
        string configPath = "messaging:googlePubSub")
    {
        builder.Services.ConfigureAndValidate(config, configPath, configure);

        builder.Services.AddSingletonAs<GooglePubSubTransport>()
            .As<IMessagingTransport>();

        return builder;
    }
}
