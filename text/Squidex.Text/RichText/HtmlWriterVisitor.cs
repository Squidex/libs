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

    public static void Render(INode node, TextWriter textWriter)
    {
        var newWriter = new HtmlTextWriter(textWriter, new string(' ', 4));

        new HtmlWriterVisitor(newWriter).Visit(node);
    }

    protected override void VisitHardBreak(INode node)
    {
        writer.WriteLine();
        writer.WriteBreak();
        writer.WriteLine();
    }

    protected override void VisitHorizontalRule(INode node)
    {
        writer.WriteFullBeginTag("hr");
    }

    protected override void VisitBlockquote(INode node)
    {
        EmbedInTag(node, "blockquote");
    }

    protected override void VisitBulletList(INode node)
    {
        EmbedInTag(node, "ul");
    }

    protected override void VisitHeading(INode node, int level)
    {
        EmbedInTag(node, $"h{level}");
    }

    protected override void VisitListItem(INode node)
    {
        EmbedInTag(node, "li");
    }

    protected override void VisitOrderedList(INode node)
    {
        EmbedInTag(node, "ol");
    }

    protected override void VisitParagraph(INode node)
    {
        EmbedInTag(node, "p");
    }

    protected override void VisitImage(INode node, string? src, string? alt, string? title)
    {
        writer.AddNonEmptyAttribute(nameof(src), src);
        writer.AddNonEmptyAttribute(nameof(alt), alt);
        writer.AddNonEmptyAttribute(nameof(title), title);
        writer.RenderBeginTag("img");
        writer.RenderEndTag();
    }

    protected override void VisitCodeBlock(INode node, string? language)
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

    protected override void VisitBold(IMark mark, Action inner)
    {
        EmbedInTag(inner, "strong");
    }

    protected override void VisitCode(IMark mark, Action inner)
    {
        EmbedInTag(inner, "code");
    }

    protected override void VisitItalic(IMark mark, Action inner)
    {
        EmbedInTag(inner, "em");
    }

    protected override void VisitUnderline(IMark mark, Action inner)
    {
        EmbedInTag(inner, "u");
    }

    protected override void VisitLink(IMark mark, Action inner, string? href, string? target)
    {
        writer.WriteBeginTag("a");
        writer.WriteNonEmptyAttribute(nameof(href), href);
        writer.WriteNonEmptyAttribute(nameof(target), target);
        writer.Write(">");
        base.VisitLink(mark, inner, href, target);
        writer.WriteEndTag("a");
    }

    protected override void VisitText(INode node)
    {
        writer.Write(node.Text ?? string.Empty);
    }

    private void EmbedInTag(INode node, string tag)
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
