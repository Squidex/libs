﻿// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Xunit;

namespace Squidex.Messaging;

[Trait("Category", "Dependencies")]
public class MongoSubscriptionStoreTests(MongoMessagingFixture fixture)
    : SubscriptionStoreTestsBase, IClassFixture<MongoMessagingFixture>
{
    protected override void Configure(MessagingBuilder builder)
    {
        builder.Services.AddSingleton(fixture.Database);

        builder.AddMongoDataStore(TestHelpers.Configuration);
    }
}
