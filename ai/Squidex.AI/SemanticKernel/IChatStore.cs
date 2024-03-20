// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

namespace Squidex.AI.SemanticKernel;

public interface IChatStore
{
    Task ClearAsync(string conversationId,
        CancellationToken ct);

    Task StoreAsync(string conversationId, string value, DateTime expires,
        CancellationToken ct);

    Task<string?> GetAsync(string conversationId,
        CancellationToken ct);
}
