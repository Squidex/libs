// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

namespace Squidex.AI;

public interface IChatToolProvider
{
    IAsyncEnumerable<IChatTool> GetToolsAsync(ChatContext chatContext,
        CancellationToken ct);
}
