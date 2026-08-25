// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System.Text;
using HtmlPerformanceKit;

namespace Squidex.Text;

public static class HtmlExtensions
{
    public static string Html2Text(this string html)
    {
        var htmlWriter = new StringBuilder();
        var htmlReader = new HtmlReader(new StringReader(html));

        WriteTextTo(htmlReader, htmlWriter);

        static bool IsTrimmed(char value)
        {
            return value == ' ' || value == '\n' || value == '\r';
        }

        // Trim within the builder.
        // Building the full string first and then trimming it would allocate
        // the whole result twice.
        var trimStart = 0;
        var trimEnd = htmlWriter.Length;

        while (trimStart < trimEnd && IsTrimmed(htmlWriter[trimStart]))
        {
            trimStart++;
        }

        while (trimEnd > trimStart && IsTrimmed(htmlWriter[trimEnd - 1]))
        {
            trimEnd--;
        }

        return htmlWriter.ToString(trimStart, trimEnd - trimStart);
    }

    private static void WriteTextTo(HtmlReader reader, StringBuilder sb)
    {
        var readText = true;
        while (reader.Read())
        {
            switch (reader.TokenKind)
            {
                case HtmlTokenKind.Text when readText:
                    var text = reader.TextAsMemory.Trim();

                    if (text.Length > 0)
                    {
                        HtmlEntity.Decode(text, sb);
                    }

                    break;

                case HtmlTokenKind.Tag:
                    var tag = reader.NameAsMemory.Span;

                    readText &= !tag.Equals("script", StringComparison.OrdinalIgnoreCase) && !tag.Equals("style", StringComparison.OrdinalIgnoreCase);
                    break;

                case HtmlTokenKind.EndTag:
                    var endTag = reader.NameAsMemory.Span;

                    if (endTag.Equals("p", StringComparison.OrdinalIgnoreCase) || endTag.Equals("br", StringComparison.OrdinalIgnoreCase))
                    {
                        sb.AppendLine();
                    }

                    readText = true;
                    break;
            }
        }
    }
}
