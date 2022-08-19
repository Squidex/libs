// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

namespace Squidex.Log
{
    public interface ILogAppender
    {
        void Append(IObjectWriter writer, SemanticLogLevel logLevel, Exception? exception);
    }
}
