// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Squidex.AI.Implementation;
using Xunit;

namespace Squidex.AI;

public class MongoChatStoreTests(MongoChatStoreFixture fixture)
    : ChatStoreTests, IClassFixture<MongoChatStoreFixture>
{
    public override Task<IChatStore> CreateSutAsync()
    {
        return Task.FromResult<IChatStore>(fixture.Store);
    }
}
