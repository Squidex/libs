﻿// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

namespace Squidex.Events;

[Trait("Category", "Dependencies")]
public class GetEventStoreTests(GetEventStoreFixture fixture)
    : EventStoreTests, IClassFixture<GetEventStoreFixture>
{
    protected override Task<IEventStore> CreateSutAsync()
    {
        return Task.FromResult(fixture.Store);
    }
}
