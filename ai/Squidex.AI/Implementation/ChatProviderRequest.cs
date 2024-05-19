// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

namespace Squidex.AI.Implementation;

public sealed class ChatProviderRequest
{
    required public ChatContext Context { get; init; }

    required public ChatHistory History { get; init; }

    required public List<IChatTool> Tools { get; init; }

    required public string? Tool { get; set; }

    required public IChatAgent Agent { get; init; }
}
