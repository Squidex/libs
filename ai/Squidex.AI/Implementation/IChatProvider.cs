﻿// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

namespace Squidex.AI.Implementation;

public interface IChatProvider
{
    IAsyncEnumerable<InternalChatEvent> StreamAsync(ChatProviderRequest request,
        CancellationToken ct = default);
}