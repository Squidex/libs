// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Xunit;

namespace Squidex.Assets.KeyValueStore;

public class PostgresKeyValueStoreTests(PostgresKeyValueStoreFixture fixture)
    : KeyValueStoreTests, IClassFixture<PostgresKeyValueStoreFixture>
{
    protected override Task<IAssetKeyValueStore<TestValue>> CreateSutAsync()
    {
        return Task.FromResult<IAssetKeyValueStore<TestValue>>(fixture.Store);
    }
}
