﻿// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Xunit;

#pragma warning disable SA1300 // Element should begin with upper-case letter

namespace Squidex.Messaging
{
    public class MongoMessagingTests : MessagingTestsBase, IClassFixture<MongoFixture>
    {
        public MongoFixture _ { get; }

        public MongoMessagingTests(MongoFixture fixture)
        {
            _ = fixture;
        }

        protected override void ConfigureServices(IServiceCollection services, ChannelName channel, bool consume)
        {
            services
                .AddSingleton(_.Database)
                .AddMongoTransport(TestHelpers.Configuration, options =>
                {
                    options.Prefetch = 0;
                    options.PollingInterval = TimeSpan.FromSeconds(0.1);
                    options.UpdateInterval = TimeSpan.FromSeconds(0.1);

                    _.CleanCollections(x => x.StartsWith(options.CollectionName, StringComparison.OrdinalIgnoreCase));
                })
                .AddMessaging(channel, true, options =>
                {
                    options.Expires = TimeSpan.FromDays(1);
                });
        }
    }
}
