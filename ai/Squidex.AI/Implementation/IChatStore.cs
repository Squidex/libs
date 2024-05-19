// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

namespace Squidex.AI.Implementation;

public interface IChatStore
{
    Task RemoveAsync(string conversationId,
        CancellationToken ct);

    Task StoreAsync(string conversationId, string value,
        CancellationToken ct);

    Task<string?> GetAsync(string conversationId,
        CancellationToken ct);
}
