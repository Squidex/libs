// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

#pragma warning disable MA0048 // File name must match type name

namespace Squidex.AI;

public sealed class ChatMessage
{
    public string Content { get; set; }

    public int TokenCount { get; set; }

    public ChatMessageType Type { get; set; }
}

public enum ChatMessageType
{
    System,
    Assistant,
    User,
}
