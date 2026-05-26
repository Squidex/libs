// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Microsoft.Extensions.Logging;

namespace Squidex.Messaging.EntityFramework;

internal static partial class LogMessages
{
    [LoggerMessage(EventId = 1, Level = LogLevel.Warning, Message = "Transport message has no MongoDb ID.")]
    public static partial void TransportMessageHasNoMongoDbId(ILogger logger);

    [LoggerMessage(EventId = 2, Level = LogLevel.Error, Message = "Failed to put the message back into the queue '{queue}'.")]
    public static partial void FailedToPutMessageBackIntoQueue(ILogger logger, Exception exception, string queue);

    [LoggerMessage(EventId = 3, Level = LogLevel.Error, Message = "Failed to remove message from queue '{queue}'.")]
    public static partial void FailedToRemoveMessageFromQueue(ILogger logger, Exception exception, string queue);

    [LoggerMessage(EventId = 4, Level = LogLevel.Information, Message = "{collectionName}: Items reset: {count}.")]
    public static partial void ItemsReset(ILogger logger, string collectionName, int count);
}
