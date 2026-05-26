// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Microsoft.Extensions.Logging;

namespace Squidex.Messaging.Redis;

internal static partial class LogMessages
{
    [LoggerMessage(EventId = 1, Level = LogLevel.Error, Message = "Failed to deserialize message.")]
    public static partial void FailedToDeserializeMessage(ILogger logger, Exception exception);

    [LoggerMessage(EventId = 100, EventName = "RedisConnectionLog", Level = LogLevel.Debug, Message = "{message}")]
    public static partial void RedisConnectionLog(ILogger logger, string? message);
}
