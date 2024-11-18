// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Xunit;

#pragma warning disable SA1300 // Element should begin with upper-case letter

namespace Squidex.Messaging;

public class MongoMessagingPrefetchTests(MongoFixture fixture) : MessagingTestsBase, IClassFixture<MongoFixture>
{
    public MongoFixture _ { get; } = fixture;

    protected override void Configure(MessagingBuilder builder)
    {
        builder.Services.AddSingleton(_.Database);

        builder.AddMongoDataStore(TestHelpers.Configuration);
        builder.AddMongoTransport(TestHelpers.Configuration, options =>
        {
            options.Prefetch = 5;
            options.PollingInterval = TimeSpan.FromSeconds(0.1);
            options.UpdateInterval = TimeSpan.FromSeconds(0.1);

            _.CleanCollections(x => x.StartsWith(options.CollectionName, StringComparison.OrdinalIgnoreCase));
        });
    }
}
