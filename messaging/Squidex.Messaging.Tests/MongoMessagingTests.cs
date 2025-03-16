// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using TestHelpers;

namespace Squidex.Messaging;

public class MongoMessagingTests(MongoMessagingFixture fixture)
    : MessagingTestsBase, IClassFixture<MongoMessagingFixture>
{
    protected override void Configure(MessagingBuilder builder)
    {
        builder.Services.AddSingleton(fixture.MongoDatabase);

        builder.AddMongoDataStore(TestUtils.Configuration);
        builder.AddMongoTransport(TestUtils.Configuration, options =>
        {
            options.Prefetch = 0;
            options.PollingInterval = TimeSpan.FromSeconds(0.1);
            options.UpdateInterval = TimeSpan.FromSeconds(0.1);
        });
    }
}
