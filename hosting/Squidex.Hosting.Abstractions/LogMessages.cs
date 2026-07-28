// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Microsoft.Extensions.Logging;

namespace Squidex.Hosting;

internal static partial class LogMessages
{
    [LoggerMessage(EventId = 1, Level = LogLevel.Warning, Message = "Failed to execute timer.")]
    public static partial void FailedToExecuteTimer(ILogger logger, Exception exception);
}
