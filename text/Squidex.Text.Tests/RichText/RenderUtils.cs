// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Squidex.Text.RichText.Model;

namespace Squidex.Text.RichText;

internal static class RenderUtils
{
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

    public static string RenderHtml(INode node)
    {
        var htmlString = new StringWriter();

        HtmlWriterVisitor.Render(node, htmlString);

        return htmlString.ToString().TrimExpected();
    }

    public static string RenderMarkdown(INode node)
    {
        var markdownString = new StringWriter();

        MarkdownVisitor.Render(node, markdownString);

        return markdownString.ToString().TrimExpected();
    }
}
