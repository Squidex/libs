// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Microsoft.Extensions.Logging;

namespace Squidex.Caching;

internal static partial class LogMessages
{
    [LoggerMessage(EventId = 1, Level = LogLevel.Warning, Message = "Failed to refresh cache entry with key {key}.")]
    public static partial void FailedToRefreshCacheEntry(ILogger logger, Exception exception, object key);
}
