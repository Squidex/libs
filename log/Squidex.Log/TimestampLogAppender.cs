// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

namespace Squidex.Log;

public sealed class TimestampLogAppender(TimeProvider timeProvider) : ILogAppender
{
    public TimestampLogAppender()
        : this(TimeProvider.System)
    {
    }

    public void Append(IObjectWriter writer, SemanticLogLevel logLevel, Exception? exception)
    {
        writer.WriteProperty("timestamp", timeProvider.GetUtcNow().UtcDateTime);
    }
}
