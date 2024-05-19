// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

namespace Squidex.AI;

public sealed class ChatRequest
{
    public string? Prompt { get; init; }

    public string? ConversationId { get; init; }

    public string? Configuration { get; init; }

    public string? Tool { get; init; }
}
