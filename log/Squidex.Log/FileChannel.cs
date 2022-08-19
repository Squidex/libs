// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Squidex.Log.Internal;

namespace Squidex.Log
{
    public sealed class FileChannel : IDisposable, ILogChannel
    {
        private readonly FileLogProcessor processor;
        private readonly object lockObject = new object();
        private volatile bool isInitialized;

        public FileChannel(string path)
        {
            Guard.NotNullOrEmpty(path, nameof(path));

            processor = new FileLogProcessor(path);
        }

        public void Dispose()
        {
            processor.Dispose();
        }

        public void Log(SemanticLogLevel logLevel, string message)
        {
            if (!isInitialized)
            {
                lock (lockObject)
                {
                    if (!isInitialized)
                    {
                        processor.Initialize();

                        isInitialized = true;
                    }
                }
            }

            processor.EnqueueMessage(new LogMessageEntry { Message = message });
        }
    }
}
