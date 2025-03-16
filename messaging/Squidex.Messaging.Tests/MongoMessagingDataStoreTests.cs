// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using TestHelpers;
using TestHelpers.MongoDb;

#pragma warning disable MA0048 // File name must match type name

namespace Squidex.Messaging;

public sealed class MongoMessagingDataStoreFixture() : MongoFixture("messagingstore-postgres")
{
    protected override void AddServices(IServiceCollection services)
    {
        services
            .AddMessaging()
            .AddMongoDataStore(TestUtils.Configuration);
    }
}

public class MongoMessagingDataStoreTests(MongoMessagingDataStoreFixture fixture)
    : MessagingDataStoreTests, IClassFixture<MongoMessagingDataStoreFixture>
{
    protected override Task<IMessagingDataStore> CreateSutAsync()
    {
        var store = fixture.Services.GetRequiredService<IMessagingDataStore>();
        return Task.FromResult(store);
    }
}
