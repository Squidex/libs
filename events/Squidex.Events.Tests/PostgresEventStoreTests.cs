// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Xunit;

namespace Squidex.Events;

public class PostgresEventStoreTests(PostgresEventStoreFixture fixture)
    : EFEventStoreTests, IClassFixture<PostgresEventStoreFixture>
{
    public override IServiceProvider Services => fixture.Services;

    protected override Task<IEventStore> CreateSutAsync()
    {
        return Task.FromResult(fixture.Store);
    }
}
