// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Microsoft.Extensions.Logging;

namespace Squidex.Flows.Internal.Execution;

internal static partial class LogMessages
{
    [LoggerMessage(EventId = 1, Level = LogLevel.Error, Message = "Failed to execute {callback}.")]
    public static partial void FailedToExecuteCallback(ILogger logger, Exception exception, object callback);

    [LoggerMessage(EventId = 2, Level = LogLevel.Error, Message = "Failed to query rule events.")]
    public static partial void FailedToQueryRuleEvents(ILogger logger, Exception exception);

    [LoggerMessage(EventId = 3, Level = LogLevel.Error, Message = "Failed to execute flow.")]
    public static partial void FailedToExecuteFlow(ILogger logger, Exception exception);
}
