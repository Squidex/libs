// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Microsoft.Extensions.Logging;

namespace Squidex.Messaging.Implementation;

internal static partial class LogMessages
{
    [LoggerMessage(EventId = 1, Level = LogLevel.Error, Message = "Failed to deserialize message with type {type}.")]
    public static partial void FailedToDeserializeMessage(ILogger logger, Exception exception, string type);

    [LoggerMessage(EventId = 2, Level = LogLevel.Information, Message = "Handling {message} for {channel} with {handler}.")]
    public static partial void HandlingMessage(ILogger logger, object? message, ChannelName channel, object handler);

    [LoggerMessage(EventId = 3, Level = LogLevel.Information, Message = "Handled {message} for {channel} with {handler}.")]
    public static partial void HandledMessage(ILogger logger, object? message, ChannelName channel, object handler);

    [LoggerMessage(EventId = 4, Level = LogLevel.Information, Message = "Failed to handle {message} for {channel} with {handler}.")]
    public static partial void FailedToHandleMessage(ILogger logger, Exception exception, object? message, ChannelName channel, object handler);

    [LoggerMessage(EventId = 5, Level = LogLevel.Error, Message = "Failed to consume message with type {type}.")]
    public static partial void FailedToConsumeMessage(ILogger logger, Exception exception, string type);

    [LoggerMessage(EventId = 6, Level = LogLevel.Warning, Message = "Message has no type header.")]
    public static partial void MessageHasNoTypeHeader(ILogger logger);
}
