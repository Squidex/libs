// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Microsoft.Extensions.Logging;

namespace Squidex.Assets.ResizeService;

internal static partial class LogMessages
{
    [LoggerMessage(EventId = 1, Level = LogLevel.Error, Message = "Failed to orient image.")]
    public static partial void FailedToOrientImage(ILogger logger, Exception exception);

    [LoggerMessage(EventId = 2, Level = LogLevel.Error, Message = "Failed to resize image.")]
    public static partial void FailedToResizeImage(ILogger logger, Exception exception);
}
