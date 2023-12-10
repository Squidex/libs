// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System.Text;
using Squidex.Text.RichText.Model;
using Xunit;

namespace Squidex.Text.RichText;

internal static class RenderUtils
{
    public static void AssertNode(INode node, string markdown, string html, string? minHtml = null, string? text = null)
    {
        var (actualMarkdown, actualHtmlFormatted) = Render(node);

        Assert.Equal(markdown.TrimExpected(), actualMarkdown);

        Assert.Equal(html.TrimExpected(), actualHtmlFormatted);

        if (minHtml != null)
        {
            var actualHtmlCompressed = RenderHtml(node, 0);

            Assert.Equal(minHtml.TrimExpected(), actualHtmlCompressed);
        }

        if (text != null)
        {
            var actualPlain = RenderText(node);

            Assert.Equal(text.TrimExpected(), actualPlain);
        }
    }

    public static (string Markdown, string Html) Render(INode node)
    {
        return (RenderMarkdown(node), RenderHtml(node));
    }

    public static string TrimExpected(this string result)
    {
        result = result.TrimStart('\n', '\r');
        result = result.TrimEnd();
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
