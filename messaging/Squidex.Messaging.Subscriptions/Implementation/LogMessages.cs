// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Microsoft.Extensions.Logging;

namespace Squidex.Messaging.Subscriptions.Implementation;

internal static partial class LogMessages
{
    [LoggerMessage(EventId = 1, Level = LogLevel.Debug, Message = "Received payload of type {type} from {sender}")]
    public static partial void ReceivedPayload(ILogger logger, Type? type, string? sender);
}
