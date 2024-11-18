// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

namespace Squidex.Log;

public sealed class TimestampLogAppender(Func<DateTime>? clock = null) : ILogAppender
{
    private readonly Func<DateTime> clock = clock ?? (() => DateTime.UtcNow);

    public TimestampLogAppender()
        : this(null)
    {
    }

    public void Append(IObjectWriter writer, SemanticLogLevel logLevel, Exception? exception)
    {
        writer.WriteProperty("timestamp", clock());
    }
}
