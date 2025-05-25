// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

#pragma warning disable MA0048 // File name must match type name

namespace Squidex.AI;

public abstract record InternalChatEvent
{
}

public abstract record ChatEvent : InternalChatEvent
{
}

public sealed record ChatHistoryLoaded : ChatEvent
{
    public ChatMessage Message { get; set; }
}

public sealed record ToolStartEvent : ChatEvent
{
    required public IChatTool Tool { get; init; }

    required public Dictionary<string, ToolValue> Arguments { get; set; }
}

public sealed record ToolEndEvent : ChatEvent
{
    required public IChatTool Tool { get; init; }

    required public string Result { get; set; }
}

public sealed record MetadataEvent : ChatEvent
{
    required public ChatMetadata Metadata { get; init; }
}

public sealed record ChunkEvent : ChatEvent
{
    required public string Content { get; init; }
}

public sealed record ChatFinishEvent : InternalChatEvent
{
    required public int NumInputTokens { get; init; }

    required public int NumOutputTokens { get; init; }
}
