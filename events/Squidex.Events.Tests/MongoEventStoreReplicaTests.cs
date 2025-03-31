// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Xunit;

namespace Squidex.Events;

public class MongoEventStoreReplicaTests(MongoEventStoreReplicaFixture fixture)
    : EventStoreTests, IClassFixture<MongoEventStoreReplicaFixture>
{
    protected override Task<IEventStore> CreateSutAsync()
    {
        return Task.FromResult(fixture.Store);
    }
}
