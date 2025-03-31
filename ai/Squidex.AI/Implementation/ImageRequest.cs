// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

namespace Squidex.AI.Implementation;

public sealed class ImageRequest
{
    required public ChatContext Context { get; init; }

    required public Dictionary<string, string> ToolData { get; init; }

    required public string Query { get; init; }

    required public IChatAgent ChatAgent { get; init; }
}
