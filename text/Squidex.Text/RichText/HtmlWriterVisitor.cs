// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System.Web.UI;
using Squidex.Text.RichText.Model;

namespace Squidex.Text.RichText;

public sealed class HtmlWriterVisitor : Visitor
{
    private readonly HtmlTextWriter writer;

    private HtmlWriterVisitor(HtmlTextWriter writer)
    {
        this.writer = writer;
    }

    public static void Render(NodeBase node, TextWriter textWriter)
    {
        var newWriter = new HtmlTextWriter(textWriter, new string(' ', 4));

        new HtmlWriterVisitor(newWriter).Visit(node);
    }

    protected override void VisitHardBreak(NodeBase node)
    {
        writer.WriteLine();
        writer.WriteBreak();
        writer.WriteLine();
    }

    protected override void VisitHorizontalLine(NodeBase node)
    {
        writer.WriteFullBeginTag("hr");
    }

    protected override void VisitBlockquote(NodeBase node)
    {
        EmbedInTag(node, "blockquote");
    }

    protected override void VisitBulletList(NodeBase node)
    {
        EmbedInTag(node, "ul");
    }

    protected override void VisitHeading(NodeBase node, int level)
    {
        EmbedInTag(node, $"h{level}");
    }

    protected override void VisitListItem(NodeBase node)
    {
        EmbedInTag(node, "li");
    }

    protected override void VisitOrderedList(NodeBase node)
    {
        EmbedInTag(node, "ol");
    }

    protected override void VisitParagraph(NodeBase node)
    {
        EmbedInTag(node, "p");
    }

    protected override void VisitImage(NodeBase node, string? src, string? alt, string? title)
    {
        writer.AddNonEmptyAttribute(nameof(src), src);
        writer.AddNonEmptyAttribute(nameof(alt), alt);
        writer.AddNonEmptyAttribute(nameof(title), title);
        writer.RenderBeginTag("img");
        writer.RenderEndTag();
    }

    protected override void VisitCodeBlock(NodeBase node, string? language)
    {
        writer.AddNonEmptyAttribute("spellcheck", "false");
        writer.AddNonEmptyAttribute("class", language, l => $"language-{l}");
        writer.RenderBeginTag("pre");

        writer.AddNonEmptyAttribute("data-code-block-lang", language);
        writer.RenderBeginTag("code");
        base.VisitCodeBlock(node, language);
        writer.RenderEndTag();

        writer.RenderEndTag();
    }

    protected override void VisitBold(MarkBase mark, Action inner)
    {
        EmbedInTag(inner, "strong");
    }

    protected override void VisitCode(MarkBase mark, Action inner)
    {
        EmbedInTag(inner, "code");
    }

    protected override void VisitItalic(MarkBase mark, Action inner)
    {
        EmbedInTag(inner, "em");
    }

    protected override void VisitUnderline(MarkBase mark, Action inner)
    {
        EmbedInTag(inner, "u");
    }

    protected override void VisitLink(MarkBase mark, Action inner, string? href, string? target)
    {
        writer.WriteBeginTag("a");
        writer.WriteNonEmptyAttribute(nameof(href), href);
        writer.WriteNonEmptyAttribute(nameof(target), target);
        writer.Write(">");
        base.VisitLink(mark, inner, href, target);
        writer.WriteEndTag("a");
    }

    protected override void VisitText(NodeBase node)
    {
        writer.Write(node.GetText());
    }

    private void EmbedInTag(NodeBase node, string tag)
    {
        writer.RenderBeginTag(tag);
        VisitChildren(node);
        writer.RenderEndTag();
    }

    private void EmbedInTag(Action inner, string tag)
    {
        writer.WriteFullBeginTag(tag);
        inner();
        writer.WriteEndTag(tag);
    }
}
