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

            writer = new StreamWriter(fs, Encoding.UTF8)
            {
                AutoFlush = true,
            };

            writer.WriteLine($"--- Started Logging {DateTime.UtcNow} ---", 1);

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
                for (var i = 1; i <= Retries; i++)
                {
                    try
                    {
                        writer.WriteLine(entry.Message);
                        break;
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
