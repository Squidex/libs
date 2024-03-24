// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Xunit;

#pragma warning disable SA1300 // Element should begin with upper-case letter

namespace Squidex.Messaging;

[Trait("Category", "Dependencies")]
public class MongoSubscriptionStoreTests : SubscriptionStoreTestsBase, IClassFixture<MongoFixture>
{
    public MongoFixture _ { get; }

    public MongoSubscriptionStoreTests(MongoFixture fixture)
    {
        _ = fixture;
    }

    protected override void Configure(MessagingBuilder builder)
    {
        builder.Services.AddSingleton(_.Database);

        builder.AddMongoDataStore(TestHelpers.Configuration);
    }
}
