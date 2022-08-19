// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

namespace Squidex.Log
{
    public sealed class ConstantsLogAppender : ILogAppender
    {
        private readonly Action<IObjectWriter> objectWriter;

        public ConstantsLogAppender(Action<IObjectWriter> objectWriter)
        {
            Guard.NotNull(objectWriter, nameof(objectWriter));

            this.objectWriter = objectWriter;
        }

        public void Append(IObjectWriter writer, SemanticLogLevel logLevel, Exception? exception)
        {
            objectWriter(writer);
        }
    }
}
