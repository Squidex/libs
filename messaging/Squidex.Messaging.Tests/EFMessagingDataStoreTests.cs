// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using TestHelpers;
using TestHelpers.EntityFramework;

#pragma warning disable MA0048 // File name must match type name

namespace Squidex.Messaging;

public sealed class EFMessagingDataStoreFixture() : PostgresFixture<TestDbContext>("messagingstore-postgres")
{
    protected override void AddServices(IServiceCollection services)
    {
        services
            .AddLogging()
            .AddMessaging()
            .AddEntityFrameworkDataStore<TestDbContext>(TestUtils.Configuration);
    }
}

public class EFMessagingDataStoreTests(EFMessagingDataStoreFixture fixture)
    : MessagingDataStoreTests, IClassFixture<EFMessagingDataStoreFixture>
{
    protected override Task<IMessagingDataStore> CreateSutAsync()
    {
        var store = fixture.Services.GetRequiredService<IMessagingDataStore>();
        return Task.FromResult(store);
    }
}
