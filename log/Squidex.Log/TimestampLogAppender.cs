// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

namespace Squidex.Log;

public sealed class TimestampLogAppender : ILogAppender
{
    private readonly Func<DateTime> clock;

    public TimestampLogAppender()
        : this(null)
    {
    }

    public TimestampLogAppender(Func<DateTime>? clock = null)
    {
        this.clock = clock ?? (() => DateTime.UtcNow);
    }

    public void Append(IObjectWriter writer, SemanticLogLevel logLevel, Exception? exception)
    {
        writer.WriteProperty("timestamp", clock());
    }
}
