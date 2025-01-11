// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Squidex.AI.Implementation;
using Xunit;

namespace Squidex.AI;

public sealed class EFChatStoreTests(EFChatStoreFixture fixture)
    : ChatStoreTests, IClassFixture<EFChatStoreFixture>
{
    public override Task<IChatStore> CreateSutAsync()
    {
        return Task.FromResult<IChatStore>(fixture.Store);
    }
}
