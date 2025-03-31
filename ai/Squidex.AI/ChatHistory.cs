// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

namespace Squidex.AI;

public sealed class ChatHistory : List<ChatMessage>
{
    public void Add(string content, ChatMessageType type)
    {
        Add(new ChatMessage { Content = content, Type = type });
    }
}
