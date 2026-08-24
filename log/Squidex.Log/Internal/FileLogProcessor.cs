// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Squidex.Log.Internal;

[ExcludeFromCodeCoverage]
public sealed class FileLogProcessor : IDisposable
{
    private const int MaxQueuedMessages = 1024;
    private const int Retries = 10;
    private readonly BlockingCollection<LogMessageEntry> messageQueue = new BlockingCollection<LogMessageEntry>(MaxQueuedMessages);
    private readonly Thread outputThread;
    private readonly string path;
    private StreamWriter? writer;

    public FileLogProcessor(string path)
    {
        this.path = path;

        outputThread = new Thread(ProcessLogQueue)
        {
            IsBackground = true,
            Name = "Logging",
        };
    }

    public void Initialize()
    {
        var fileInfo = new FileInfo(path);
        try
        {
            if (!fileInfo.Directory!.Exists)
            {
                fileInfo.Directory.Create();
            }

            var fs = new FileStream(fileInfo.FullName, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);

            // Do not flush after every write. The queue is drained by a single thread and we flush
            // as soon as it runs empty, so that bursts of messages result in one write to disk.
            writer = new StreamWriter(fs, Encoding.UTF8)
            {
                AutoFlush = false,
            };

            writer.WriteLine($"--- Started Logging {DateTime.UtcNow} ---", 1);
            writer.Flush();

            outputThread.Start();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Log directory '{fileInfo.Directory!.FullName}' does not exist or cannot be created.", ex);
        }
    }

    public void EnqueueMessage(LogMessageEntry message)
    {
        messageQueue.Add(message);
    }

    private void ProcessLogQueue()
    {
        if (writer == null)
        {
            return;
        }

        try
        {
            foreach (var entry in messageQueue.GetConsumingEnumerable())
            {
                // Only hit the disk when there is nothing left to write, so that a burst of
                // messages is written with a single flush.
                WriteWithRetries(entry.Message, messageQueue.Count == 0);
            }

            WriteWithRetries(null, true);
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

    private void WriteWithRetries(string? message, bool flush)
    {
        var isWritten = message == null;

        for (var i = 1; i <= Retries; i++)
        {
            try
            {
                // Never write the message twice when only the flush has failed before.
                if (!isWritten)
                {
                    writer!.WriteLine(message);
                    isWritten = true;
                }

                if (flush)
                {
                    writer!.Flush();
                }

                return;
            }
            catch (Exception ex)
            {
                Thread.Sleep(i * 10);

                if (i == Retries)
                {
                    Console.WriteLine($"Failed to write to log file '{path}': {ex}");
                }
            }
        }
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
            writer?.Dispose();
        }
    }
}
