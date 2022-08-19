// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

namespace Squidex.Messaging
{
    public class KafkaTests : MessagingTestsBase
    {
        protected override string TopicOrQueueName => "dev";

        protected override void ConfigureServices(IServiceCollection services, ChannelName channel, bool consume)
        {
            services
                .AddKafkaTransport(TestHelpers.Configuration)
                .AddMessaging(channel, consume, options =>
                {
                    options.Expires = TimeSpan.FromDays(1);
                });
        }
    }
}
