// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

namespace Squidex.AI.Implementation;

public interface IChatPipe
{
    IAsyncEnumerable<InternalChatEvent> StreamAsync(IAsyncEnumerable<InternalChatEvent> source, ChatProviderRequest request,
        CancellationToken ct = default);
}
