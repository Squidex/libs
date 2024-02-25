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
    private static readonly char[] TrimChars = [' ', '\n', '\r'];

    public static string Html2Text(this string html)
    {
        var htmlWriter = new StringBuilder();
        var htmlReader = new HtmlReader(new StringReader(html));

        WriteTextTo(htmlReader, htmlWriter);

        return htmlWriter.ToString().Trim(TrimChars);
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
