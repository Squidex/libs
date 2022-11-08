// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

namespace Squidex.Caching;

public sealed class CacheInvalidateMessage
{
    public Guid Source { get; init; }

    public string[] Keys { get; init; }
}
