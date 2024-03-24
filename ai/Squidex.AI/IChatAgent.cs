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

    Task StopConversationAsync(string conversationId,
        CancellationToken ct = default);

    Task<ChatBotResponse> PromptAsync(string prompt, string? conversationId = null,
        CancellationToken ct = default);
}
