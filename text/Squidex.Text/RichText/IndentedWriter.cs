// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using static System.Net.Mime.MediaTypeNames;

namespace Squidex.Text.RichText;

internal sealed class IndentedWriter
{
    private readonly StringWriter writer;
    private readonly HashSet<string> indents = new HashSet<string>();

    public IndentedWriter(StringWriter writer)
    {
        this.writer = writer;
    }

    public void WriteLine(string text)
    {
        WriteIndentsCore();

        writer.WriteLine(text);
    }

    public void WriteLine()
    {
        writer.WriteLine();
    }

    public void Write(string text)
    {
        writer.Write(text);
    }

    private void WriteIndentsCore()
    {
        foreach (var indent in indents)
        {
            writer.Write(indent);
        }
    }
}
