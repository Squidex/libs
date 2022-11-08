// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

namespace Squidex.Log.Adapter;

public sealed class CategoryNameAppender : ILogAppender
{
    private readonly string category;

    public CategoryNameAppender(string category)
    {
        this.category = category;
    }

    public void Append(IObjectWriter writer, SemanticLogLevel logLevel, Exception? exception)
    {
        writer.WriteProperty(nameof(category), category);
    }
}
