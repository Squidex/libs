// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Confluent.Kafka;
using Microsoft.Extensions.Logging;

namespace Squidex.Messaging.Kafka;

internal static partial class LogMessages
{
    [LoggerMessage(EventId = 1, Level = LogLevel.Error, Message = "Subscription failed.")]
    public static partial void SubscriptionFailed(ILogger logger, Exception exception);

    [LoggerMessage(EventId = 2, Level = LogLevel.Error, Message = "Kafka shutdown failed.")]
    public static partial void KafkaShutdownFailed(ILogger logger, Exception exception);

    [LoggerMessage(EventId = 3, Level = LogLevel.Warning, Message = "Transport message has no consume result.")]
    public static partial void TransportMessageHasNoConsumeResult(ILogger logger);

    [LoggerMessage(EventId = 4, Level = LogLevel.Error, Message = "Failed to commit the message.")]
    public static partial void FailedToCommitMessage(ILogger logger, Exception exception);

    [LoggerMessage(EventId = 5, Level = LogLevel.Information, Message = "Kafka statistics received: {stats}.")]
    public static partial void KafkaStatisticsReceived(ILogger logger, string stats);

    [LoggerMessage(EventId = 6, Level = LogLevel.Information, Message = "Kafka error with code {code} happened: {details}.")]
    public static partial void KafkaError(ILogger logger, ErrorCode code, string details);

    [LoggerMessage(EventId = 7, Message = "Kafka log received from system {system}: {message}.")]
    public static partial void KafkaLog(ILogger logger, LogLevel level, string system, string message);
}
