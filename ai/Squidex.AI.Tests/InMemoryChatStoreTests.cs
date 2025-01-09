// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Squidex.AI.Implementation;

namespace Squidex.AI;

public class InMemoryChatStoreTests : ChatStoreTests
{
    public override Task<IChatStore> CreateSutAsync()
    {
        return Task.FromResult<IChatStore>(new InMemoryChatStore());
    }
}
