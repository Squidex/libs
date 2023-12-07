// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System.Web.UI;
using Squidex.Text.RichText.Model;

namespace Squidex.Text.RichText;

public sealed class HtmlWriterVisitor : Visitor<bool>
{
    private readonly HtmlTextWriter htmlWriter;

    public HtmlWriterVisitor(HtmlTextWriter htmlWriter)
    {
        this.htmlWriter = htmlWriter;
    }

    protected override bool VisitHardBreak(Node node)
    {
        htmlWriter.WriteLine();
        htmlWriter.WriteBreak();
        htmlWriter.WriteLine();
        return true;
    }

    protected override bool VisitHorizontalLine(Node node)
    {
        htmlWriter.WriteFullBeginTag("hr");
        return true;
    }

    protected override bool VisitBlockquote(Node node)
    {
        EmbedInTag(node, "blockquote");
        return true;
    }

    protected override bool VisitBulletList(Node node)
    {
        EmbedInTag(node, "ul");
        return true;
    }

    protected override bool VisitHeading(Node node)
    {
        EmbedInTag(node, $"h{node.GetNumber("level", 1)}");
        return true;
    }

    protected override bool VisitListItem(Node node)
    {
        EmbedInTag(node, "li");
        return true;
    }

    protected override bool VisitOrderedList(Node node)
    {
        EmbedInTag(node, "ol");
        return true;
    }

    protected override bool VisitParagraph(Node node)
    {
        EmbedInTag(node, "p");
        return true;
    }

    protected override bool VisitImage(Node node)
    {
        htmlWriter.AddNonEmptyAttribute("src", node.GetString("src"));
        htmlWriter.AddNonEmptyAttribute("alt", node.GetString("alt"));
        htmlWriter.AddNonEmptyAttribute("title", node.GetString("title"));
        htmlWriter.RenderBeginTag("img");
        htmlWriter.RenderEndTag();
        return true;
    }

    protected override bool VisitCodeBlock(Node node)
    {
        var language = node.GetString("language");

        htmlWriter.AddAttribute("spellcheck", "false");
        htmlWriter.AddNonEmptyAttribute("class", language, l => $"language-{l}");
        htmlWriter.RenderBeginTag("pre");

        htmlWriter.AddNonEmptyAttribute("data-code-block-lang", language);
        htmlWriter.RenderBeginTag("code");
        base.VisitCodeBlock(node);
        htmlWriter.RenderEndTag();

        htmlWriter.RenderEndTag();
        return true;
    }

    protected override bool VisitBold(Mark mark, Func<bool> inner)
    {
        EmbedInTag(inner, "strong");
        return true;
    }

    protected override bool VisitCode(Mark mark, Func<bool> inner)
    {
        EmbedInTag(inner, "code");
        return true;
    }

    protected override bool VisitItalic(Mark mark, Func<bool> inner)
    {
        EmbedInTag(inner, "em");
        return true;
    }

    protected override bool VisitUnderline(Mark mark, Func<bool> inner)
    {
        EmbedInTag(inner, "u");
        return true;
    }

    protected override bool VisitLink(Mark mark, Func<bool> inner)
    {
        htmlWriter.AddNonEmptyAttribute("href", mark.GetString("href"));
        htmlWriter.AddNonEmptyAttribute("target", mark.GetString("target"));
        htmlWriter.RenderBeginTag("a");
        base.VisitLink(mark, inner);
        htmlWriter.RenderEndTag();
        return true;
    }

    protected override bool VisitText(Node node)
    {
        htmlWriter.Write(node.Text);
        return true;
    }

    private void EmbedInTag(Node node, string tag)
    {
        htmlWriter.RenderBeginTag(tag);
        VisitChildren(node);
        htmlWriter.RenderEndTag();
    }

    private void EmbedInTag(Func<bool> inner, string tag)
    {
        htmlWriter.WriteFullBeginTag(tag);
        inner();
        htmlWriter.WriteEndTag(tag);
    }
}
