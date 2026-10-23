// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

namespace Squidex.Caching;

public sealed class BackgroundCacheOptions
{
    public TimeSpan RetryDelay { get; set; } = TimeSpan.FromSeconds(10);
}
