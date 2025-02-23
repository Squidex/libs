// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Xunit;

namespace Squidex.Messaging;

public class EFMessagingDataStoreTests(EFMessagingDataStoreFixture fixture)
    : MessagingDataStoreTests, IClassFixture<EFMessagingDataStoreFixture>
{
    protected override Task<IMessagingDataStore> CreateSutAsync()
    {
        return Task.FromResult(fixture.Store);
    }
}
