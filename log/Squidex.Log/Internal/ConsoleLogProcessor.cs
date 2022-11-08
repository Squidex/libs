// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace Squidex.Log.Internal;

[ExcludeFromCodeCoverage]
public sealed class ConsoleLogProcessor : IDisposable
{
    private const int MaxQueuedMessages = 1024;
    private readonly IConsole console;
    private readonly BlockingCollection<LogMessageEntry> messageQueue = new BlockingCollection<LogMessageEntry>(MaxQueuedMessages);
    private readonly Thread outputThread;

    public ConsoleLogProcessor()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            console = new WindowsLogConsole(true);
        }
        else
        {
            console = new AnsiLogConsole(false);
        }

        outputThread = new Thread(ProcessLogQueue)
        {
            IsBackground = true, Name = "Logging"
        };

        outputThread.Start();
    }

    public void EnqueueMessage(LogMessageEntry message)
    {
        if (!messageQueue.IsAddingCompleted)
        {
            try
            {
                messageQueue.Add(message);
                return;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to enqueue log message: {ex}.");
            }
        }

        WriteMessage(message);
    }

    private void ProcessLogQueue()
    {
        try
        {
            foreach (var message in messageQueue.GetConsumingEnumerable())
            {
                WriteMessage(message);
            }
        }
        catch
        {
            try
            {
                messageQueue.CompleteAdding();
            }
            catch
            {
                return;
            }
        }
    }

    private void WriteMessage(LogMessageEntry entry)
    {
        console.WriteLine(entry.Color, entry.Message);
    }

    public void Dispose()
    {
        messageQueue.CompleteAdding();

        try
        {
            outputThread.Join(1500);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to shutdown log queue grateful: {ex}.");
        }
        finally
        {
            console.Reset();
        }
    }
}
