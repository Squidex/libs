// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

namespace Squidex.Messaging;

public class GoogleCloudTests : MessagingTestsBase
{
    protected override string TopicOrQueueName => "messaging-tests";

    protected override bool CanHandleAndSimulateTimeout => false;

    protected override void ConfigureServices(IServiceCollection services, ChannelName channel, bool consume)
    {
        services
            .AddGooglePubSubTransport(TestHelpers.Configuration)
            .AddMessaging(channel, consume, options =>
            {
                options.Expires = TimeSpan.FromDays(1);
            });
    }
}
