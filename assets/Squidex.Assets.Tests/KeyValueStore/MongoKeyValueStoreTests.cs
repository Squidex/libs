// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using TestHelpers.MongoDb;

#pragma warning disable MA0048 // File name must match type name

namespace Squidex.Assets.KeyValueStore;

public sealed class MongoKeyValueStoreFixture() : MongoFixture("asset-kvp-mongo")
{
    protected override void AddServices(IServiceCollection services)
    {
        services.AddMongoAssetKeyValueStore();
    }
}

public class MongoKeyValueStoreTests(MongoKeyValueStoreFixture fixture)
    : KeyValueStoreTests, IClassFixture<MongoKeyValueStoreFixture>
{
    protected override Task<IAssetKeyValueStore<KeyValueTestData>> CreateSutAsync()
    {
        var store = fixture.Services.GetRequiredService<IAssetKeyValueStore<KeyValueTestData>>();
        return Task.FromResult(store);
    }
}
