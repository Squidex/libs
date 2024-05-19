// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

namespace Squidex.AI.Implementation;

public sealed class NoopChatProvider : IChatProvider
{
    public IAsyncEnumerable<InternalChatEvent> StreamAsync(ChatProviderRequest request,
        CancellationToken ct = default)
    {
        return AsyncEnumerable.Empty<InternalChatEvent>();
    }
}
