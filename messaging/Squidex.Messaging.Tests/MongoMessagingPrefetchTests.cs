// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Xunit;

#pragma warning disable SA1300 // Element should begin with upper-case letter

namespace Squidex.Messaging
{
    public class MongoMessagingPrefetchTests : MessagingTestsBase, IClassFixture<MongoFixture>
    {
        public MongoFixture _ { get; }

        public MongoMessagingPrefetchTests(MongoFixture fixture)
        {
            _ = fixture;
        }

        protected override void ConfigureServices(IServiceCollection services, ChannelName channel, bool consume)
        {
            services
                .AddSingleton(_.Database)
                .AddMongoTransport(TestHelpers.Configuration, options =>
                {
                    options.Prefetch = 5;
                    options.PollingInterval = TimeSpan.FromSeconds(0.1);
                    options.UpdateInterval = TimeSpan.FromSeconds(0.1);

                    _.CleanCollections(x => x.StartsWith(options.CollectionName, StringComparison.OrdinalIgnoreCase));
                })
                .AddMessaging(channel, consume, options =>
                {
                    options.Expires = TimeSpan.FromDays(1);
                });
        }
    }
}
