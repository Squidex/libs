// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System.Text;
using HtmlPerformanceKit;
using Microsoft.Extensions.ObjectPool;
using Squidex.Text.Internal;

namespace Squidex.Text;

public static class HtmlExtensions
{
    private static readonly HashSet<string> BlockElements = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "address",
        "article",
        "aside",
        "blockquote",
        "br",
        "dd",
        "div",
        "dl",
        "dt",
        "figcaption",
        "figure",
        "footer",
        "form",
        "h1",
        "h2",
        "h3",
        "h4",
        "h5",
        "h6",
        "header",
        "hr",
        "li",
        "main",
        "nav",
        "ol",
        "p",
        "pre",
        "section",
        "table",
        "td",
        "th",
        "tr",
        "ul",
    };

    // Lookup the tag names without allocating strings.
    private static readonly HashSet<string>.AlternateLookup<ReadOnlySpan<char>> BlockElementsLookup =
        BlockElements.GetAlternateLookup<ReadOnlySpan<char>>();

    // Also keep larger builders, because the text of a document is usually larger than the default limit.
    private static readonly ObjectPool<StringBuilder> StringBuilders =
        new DefaultObjectPoolProvider().CreateStringBuilderPool(1024, 64 * 1024);

    public static string Html2Text(this string html)
    {
        var htmlWriter = StringBuilders.Get();
        try
        {
            using (var lease = HtmlReaderPool.Rent(html))
            {
                WriteTextTo(lease.Reader, htmlWriter);
            }

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
        finally
        {
            StringBuilders.Return(htmlWriter);
        }
    }

    private static void WriteTextTo(HtmlReader reader, StringBuilder sb)
    {
        var readText = true;
        while (reader.Read())
        {
            switch (reader.TokenKind)
            {
                case HtmlTokenKind.Text when readText:
                    // The reader has already decoded the entities.
                    WriteText(reader.TextAsMemory.Span, sb);
                    break;

                case HtmlTokenKind.Tag:
                    var tag = reader.NameAsMemory.Span;

                    if (IsCode(tag))
                    {
                        readText = false;
                    }
                    else if (BlockElementsLookup.Contains(tag))
                    {
                        WriteLine(sb);
                    }

                    break;

                case HtmlTokenKind.EndTag:
                    var endTag = reader.NameAsMemory.Span;

                    if (IsCode(endTag))
                    {
                        readText = true;
                    }
                    else if (BlockElementsLookup.Contains(endTag))
                    {
                        WriteLine(sb);
                    }

                    break;
            }
        }
    }

    private static void WriteText(ReadOnlySpan<char> text, StringBuilder sb)
    {
        var wordStart = 0;

        for (var i = 0; i <= text.Length; i++)
        {
            if (i < text.Length && !char.IsWhiteSpace(text[i]))
            {
                continue;
            }

            // Append the words at once and not char by char.
            if (i > wordStart)
            {
                sb.Append(text[wordStart..i]);
            }

            // Collapse whitespaces like the browser does, but keep them between inline elements.
            if (i < text.Length && sb.Length > 0 && !char.IsWhiteSpace(sb[^1]))
            {
                sb.Append(' ');
            }

            wordStart = i + 1;
        }
    }

    private static void WriteLine(StringBuilder sb)
    {
        // The whitespace before the line break is not needed.
        if (sb.Length > 0 && sb[^1] == ' ')
        {
            sb.Length--;
        }

        if (sb.Length > 0 && sb[^1] != '\n')
        {
            sb.AppendLine();
        }
    }

    private static bool IsCode(ReadOnlySpan<char> tag)
    {
        return tag.Equals("script", StringComparison.OrdinalIgnoreCase) || tag.Equals("style", StringComparison.OrdinalIgnoreCase);
    }
}
