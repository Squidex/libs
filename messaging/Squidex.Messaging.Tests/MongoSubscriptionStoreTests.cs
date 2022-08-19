﻿// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Microsoft.Extensions.Options;
using Squidex.Messaging.Implementation;
using Squidex.Messaging.Mongo;
using Xunit;

#pragma warning disable SA1300 // Element should begin with upper-case letter

namespace Squidex.Messaging
{
    public class MongoSubscriptionStoreTests : SubscriptionStoreTestsBase, IClassFixture<MongoFixture>
    {
        public MongoFixture _ { get; }

        public MongoSubscriptionStoreTests(MongoFixture fixture)
        {
            _ = fixture;
        }

        public async override Task<ISubscriptionStore> CreateSubscriptionStoreAsync()
        {
            var options = Options.Create(new MongoSubscriptionStoreOptions());

            _.CleanCollections(x => x == options.Value.CollectionName);

            var sut = new MongoSubscriptionStore(_.Database, options);

            await sut.InitializeAsync(default);

            return sut;
        }
    }
}
