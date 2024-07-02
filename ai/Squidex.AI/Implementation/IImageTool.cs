// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

namespace Squidex.AI.Implementation;

public interface IImageTool : IChatTool
{
    Task<string> GenerateAsync(ToolContext toolContext,
        CancellationToken ct);
}
