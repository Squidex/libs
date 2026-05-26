// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Microsoft.Extensions.Logging;

namespace Squidex.Messaging.RabbitMq;

internal static partial class LogMessages
{
    [LoggerMessage(EventId = 1, Level = LogLevel.Error, Message = "Failed to handle message from queue {queue}.")]
    public static partial void FailedToHandleMessageFromQueue(ILogger logger, Exception exception, string queue);

    [LoggerMessage(EventId = 2, Level = LogLevel.Warning, Message = "Transport message has no RabbitMq delivery tag.")]
    public static partial void TransportMessageHasNoRabbitMqDeliveryTag(ILogger logger);

    [LoggerMessage(EventId = 3, Level = LogLevel.Error, Message = "Failed to acknowledge message from queue {queue}.")]
    public static partial void FailedToAcknowledgeMessageFromQueue(ILogger logger, Exception exception, string queue);

    [LoggerMessage(EventId = 4, Level = LogLevel.Error, Message = "Failed to reject message from queue {queue}.")]
    public static partial void FailedToRejectMessageFromQueue(ILogger logger, Exception exception, string queue);
}
