// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

namespace Squidex.Log.Adapter;

public sealed class CategoryNameAppender(string category) : ILogAppender
{
    public void Append(IObjectWriter writer, SemanticLogLevel logLevel, Exception? exception)
    {
        writer.WriteProperty(nameof(category), category);
    }
}
