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
        var sb = new StringBuilder();

        using (var reader = new HtmlReader(new StringReader(html)))
        {
            WriteTextTo(reader, sb);
        }

        return sb.ToString().Trim(TrimChars);
    }

    private static void WriteTextTo(HtmlReader reader, StringBuilder sb)
    {
        var readText = true;
        while (reader.Read())
        {
            switch (reader.TokenKind)
            {
                case HtmlTokenKind.Text when readText:
                    var text = reader.Text;

                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        HtmlEntity.Decode(text, sb);
                    }

                    break;

                case HtmlTokenKind.Tag:
                    readText &= reader.Name != "script" && reader.Name != "style";
                    break;

                case HtmlTokenKind.EndTag:
                    if (reader.Name == "p" || reader.Name == "br")
                    {
                        sb.AppendLine();
                    }

                    readText = true;
                    break;
            }
        }
    }
}
