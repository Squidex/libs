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
    public static IServiceCollection AddGooglePubSubTransport(this IServiceCollection services, IConfiguration config, Action<GooglePubSubTransportOptions>? configure = null,
        string configPath = "messaging:googlePubSub")
    {
        services.ConfigureAndValidate(config, configPath, configure);

        services.AddSingletonAs<GooglePubSubTransport>()
            .As<IMessagingTransport>();

        return services;
    }
}
