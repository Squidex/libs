// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Squidex.Log.Internal;

namespace Squidex.Log;

public sealed class ConsoleLogChannel(bool useColors = false) : ILogChannel, IDisposable
{
    private readonly ConsoleLogProcessor processor = new ConsoleLogProcessor();

    public void Dispose()
    {
        processor.Dispose();
    }

    public void Log(SemanticLogLevel logLevel, string message)
    {
        var color = 0;

        if (useColors)
        {
            if (logLevel == SemanticLogLevel.Warning)
            {
                color = 0xffff00;
            }
            else if (logLevel >= SemanticLogLevel.Error)
            {
                color = 0xff0000;
            }
        }

        processor.EnqueueMessage(new LogMessageEntry { Message = message, Color = color });
    }
}
