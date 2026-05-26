// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Microsoft.Extensions.Logging;

namespace Squidex.Flows.CronJobs.Internal;

internal static partial class LogMessages
{
    [LoggerMessage(EventId = 1, Level = LogLevel.Warning, Message = "Failed to parse expression '{expression}' for id '{id}'")]
    public static partial void FailedToParseExpression(ILogger logger, string expression, string id);

    [LoggerMessage(EventId = 2, Level = LogLevel.Warning, Message = "Failed to get next occurrence for cron job '{id}' and expression '{expression}'")]
    public static partial void FailedToGetNextOccurrency(ILogger logger, string id, string expression);

    [LoggerMessage(EventId = 3, Level = LogLevel.Error, Message = "Failed to handle cron job with id '{id}'")]
    public static partial void FailedToHandleCronJob(ILogger logger, Exception exception, string id);

    [LoggerMessage(EventId = 4, Level = LogLevel.Critical, Message = "Failed to reschedule cron jobs.")]
    public static partial void FailedToRescheduleCronJobs(ILogger logger, Exception exception);
}
