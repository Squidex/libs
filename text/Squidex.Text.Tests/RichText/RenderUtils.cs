// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System.Text;
using Squidex.Text.RichText.Model;

namespace Squidex.Text.RichText;

internal static class RenderUtils
{
    public static void AssertNode(INode node, string? markdown = null, string? html = null, string? minHtml = null, string? text = null)
    {
        if (markdown != null)
        {
            var actual = RenderMarkdown(node);

            Assert.Equal(markdown.TrimExpected(), actual);
        }

        if (html != null)
        {
            var actual = RenderHtml(node, 4);

            Assert.Equal(html.TrimExpected(), actual);
        }

        if (minHtml != null)
        {
            var actual = RenderHtml(node, 0);

            Assert.Equal(minHtml.TrimExpected(), actual);
        }

        if (text != null)
        {
            var actual = RenderText(node);

            Assert.Equal(text.TrimExpected(), actual);
        }
    }

    public static (string Markdown, string Html) Render(INode node)
    {
        return (RenderMarkdown(node), RenderHtml(node));
    }

    public static string TrimExpected(this string result)
    {
        result = result.TrimStart('\n', '\r');
        result = result.Replace("\r\n", "\n", StringComparison.Ordinal);

        return result;
    }

    public static string RenderHtml(INode node, int indentation = 4)
    {
        var sb = new StringBuilder();

        HtmlWriterVisitor.Render(node, sb, new HtmlWriterOptions { Indentation = indentation });

        return sb.ToString().TrimExpected();
    }

    public static string RenderMarkdown(INode node)
    {
        var sb = new StringBuilder();

        MarkdownVisitor.Render(node, sb);

        return sb.ToString().TrimExpected();
    }

    public static string RenderText(INode node, int maxLength = int.MaxValue)
    {
        var sb = new StringBuilder();

        TextVisitor.Render(node, sb, maxLength);

        return sb.ToString().TrimExpected();
    }
}
