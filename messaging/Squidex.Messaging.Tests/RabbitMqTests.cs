// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

namespace Squidex.Messaging
{
    public class RabbitMqTests : MessagingTestsBase
    {
        protected override string TopicOrQueueName => "dev";

        protected override bool CanHandleAndSimulateTimeout => false;

        protected override void ConfigureServices(IServiceCollection services, ChannelName channel, bool consume)
        {
            services
                .AddRabbitMqTransport(TestHelpers.Configuration)
                .AddMessaging(channel, consume, options =>
                {
                    options.Expires = TimeSpan.FromDays(1);
                });
        }
    }
}
