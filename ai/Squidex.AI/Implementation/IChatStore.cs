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

    Task StoreAsync(string conversationId, Conversation conversation, DateTime now,
        CancellationToken ct);

    Task<Conversation?> GetAsync(string conversationId,
        CancellationToken ct);

    IAsyncEnumerable<(string Id, Conversation Value)> QueryAsync(DateTime olderThan,
        CancellationToken ct);
}
