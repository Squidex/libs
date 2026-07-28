// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Microsoft.Extensions.Logging;

namespace Squidex.Assets.FTP;

internal static partial class LogMessages
{
    [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "Initialized with {path}")]
    public static partial void InitializedWithPath(ILogger logger, string path);
}
