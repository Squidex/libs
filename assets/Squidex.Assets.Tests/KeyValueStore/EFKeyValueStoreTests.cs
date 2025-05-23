// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Microsoft.EntityFrameworkCore;
using TestHelpers.EntityFramework;

#pragma warning disable MA0048 // File name must match type name

namespace Squidex.Assets.KeyValueStore;

public class EFKeyValueStoreDbContext(DbContextOptions options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.UseAssetKeyValueStore<KeyValueTestData>();
        base.OnModelCreating(modelBuilder);
    }
}

public sealed class EFKeyValueStoreFixture() : PostgresFixture<EFKeyValueStoreDbContext>("asset-kvp-postgres")
{
    protected override void AddServices(IServiceCollection services)
    {
        services.AddEntityFrameworkAssetKeyValueStore<EFKeyValueStoreDbContext, KeyValueTestData>();
    }
}

public class EFKeyValueStoreTests(EFKeyValueStoreFixture fixture)
    : KeyValueStoreTests, IClassFixture<EFKeyValueStoreFixture>
{
    protected override Task<IAssetKeyValueStore<KeyValueTestData>> CreateSutAsync()
    {
        var store = fixture.Services.GetRequiredService<IAssetKeyValueStore<KeyValueTestData>>();
        return Task.FromResult(store);
    }
}
