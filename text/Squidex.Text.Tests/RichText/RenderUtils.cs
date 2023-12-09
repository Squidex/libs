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
    public static void AssertNode(INode node, string expectedMarkdown, string expectedFormattedHtml, string? expectedCompressedHtml)
    {
        var (markdown, htmlFormatted, htmlCompressed) = Render(node);

        Assert.Equal(expectedMarkdown.TrimExpected(), markdown);
        Assert.Equal(expectedFormattedHtml.TrimExpected(), htmlFormatted);

        if (expectedCompressedHtml != null)
        {
            Assert.Equal(expectedCompressedHtml.TrimExpected(), htmlCompressed);
        }
    }

    public static (string Markdown, string Html, string CompressedHtml) Render(INode node)
    {
        return (RenderMarkdown(node), RenderHtml(node), RenderHtml(node, 0));
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
}
