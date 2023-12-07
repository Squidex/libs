// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Web.UI;
using System.Xml;
using Markdig.Renderers.Normalize;
using Squidex.Text.RichText.Model;

namespace Squidex.Text.RichText;

internal static class RenderUtils
{
    public static (string Markdown, string Html) Render(Node node)
    {
        return (RenderMarkdown(node), RenderHtml(node));
    }

    public static string TrimExpected(this string result)
    {
        result = result.TrimStart('\n', '\r');
        result = result.TrimEnd();
        result = result.Replace("\r\n", "\n");
        return result;
    }

    private static string RenderHtml(Node node)
    {
        var htmlString = new StringWriter();
        var htmlWriter = new HtmlTextWriter(htmlString, new string(' ', 4));

        new HtmlWriterVisitor(htmlWriter).Visit(node);

        return htmlString.ToString().TrimExpected();
    }

    private static string RenderMarkdown(Node node)
    {
        var markdownString = new StringWriter();
        var markdownWriter = new NormalizeRenderer(markdownString);

        new MarkdownVisitor2(markdownWriter).Visit(node);

        return markdownString.ToString().TrimExpected();
    }
}
