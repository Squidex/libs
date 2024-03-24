// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Microsoft.Extensions.Configuration;
using Squidex.Messaging;
using Squidex.Messaging.Kafka;

namespace Microsoft.Extensions.DependencyInjection;

public static class MessagingServiceExtensions
{
    public static MessagingBuilder AddKafkaTransport(this MessagingBuilder builder, IConfiguration config, Action<KafkaTransportOptions>? configure = null,
        string configPath = "messaging:kafka")
    {
        builder.Services.ConfigureAndValidate(config, configPath, configure);

        builder.Services.AddSingletonAs<KafkaOwner>()
            .AsSelf();

        builder.Services.AddSingletonAs<KafkaTransport>()
            .As<IMessagingTransport>();

        return builder;
    }
}
