// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Xunit;

namespace Squidex.Events;

public class MongoEventStoreStandaloneTests(MongoEventStoreStandaloneFixture fixture)
    : EventStoreTests, IClassFixture<MongoEventStoreStandaloneFixture>
{
    protected override Task<IEventStore> CreateSutAsync()
    {
        return Task.FromResult(fixture.Store);
    }
}
