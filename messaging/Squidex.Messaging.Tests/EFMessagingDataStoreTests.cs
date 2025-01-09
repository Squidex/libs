// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Xunit;

namespace Squidex.Messaging;

public class EFMessagingDataStoreTests(MongoMessagingDataStoreFixture fixture)
    : MessagingDataStoreTests, IClassFixture<MongoMessagingDataStoreFixture>
{
    protected override Task<IMessagingDataStore> CreateSutAsync()
    {
        return Task.FromResult(fixture.Store);
    }
}
