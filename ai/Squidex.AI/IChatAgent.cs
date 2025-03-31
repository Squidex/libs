// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

namespace Squidex.AI;

public interface IChatAgent
{
    bool IsConfigured { get; }

    Task StopConversationAsync(string conversationId, ChatContext? context = null,
        CancellationToken ct = default);

    Task<ChatResult> PromptAsync(ChatRequest request, ChatContext? context = null,
        CancellationToken ct = default);

    IAsyncEnumerable<ChatEvent> StreamAsync(ChatRequest request, ChatContext? context = null,
        CancellationToken ct = default);
}
