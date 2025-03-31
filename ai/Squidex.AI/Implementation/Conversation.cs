// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

namespace Squidex.AI.Implementation;

public sealed class Conversation
{
    public ChatHistory History { get; init; }

    public Dictionary<string, string> ToolData { get; init; }
}
