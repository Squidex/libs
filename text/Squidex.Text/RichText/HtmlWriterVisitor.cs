// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System.Net;
using System.Text;
using Squidex.Text.RichText.Model;
using Squidex.Text.RichText.Writer;

namespace Squidex.Text.RichText;

public sealed class HtmlWriterVisitor : Visitor
{
    private static readonly string Indent2 = "  ";
    private static readonly string Indent4 = "    ";
    private static readonly string[] Headings =
        [
            "h1",
            "h2",
            "h3",
            "h4",
            "h5",
            "h6"
        ];

    private readonly List<(string Key, string? Value)> attributes = [];
    private readonly IWriter writer;
    private readonly string indent;
    private bool hasBlockChildren;

    private HtmlWriterVisitor(IWriter writer, string indent)
    {
        this.writer = writer;
        this.indent = indent;
    }

    public static void Render(INode node, StringBuilder stringBuilder, HtmlWriterOptions options)
    {
        var newIndent = options.Indentation switch
        {
            0 => string.Empty,
            2 => Indent2,
            4 => Indent4,
            _ => new string(' ', options.Indentation)
        };

        IWriter newWriter = options.Indentation > 0 ? new IndentedWriter(stringBuilder) : new PlainWriter(stringBuilder);

        new HtmlWriterVisitor(newWriter, newIndent).Visit(node);
    }

    protected override void VisitImage(INode node, string? src, string? alt, string? title)
    {
        attributes.Add((nameof(alt), alt));
        attributes.Add((nameof(src), src));
        attributes.Add((nameof(title), title));

        writer.Write("<img");
        WriteAttributes();
        writer.Write(">");
    }

    protected override void VisitCodeBlock(INode node, string? language)
    {
        if (!string.IsNullOrWhiteSpace(language))
        {
            attributes.Add(("class", $"language-{language}"));
        }

        RenderBlock(node, (self: this, node, language), "pre", static s =>
        {
            if (!string.IsNullOrWhiteSpace(s.language))
            {
                s.self.attributes.Add(("data-code-block-language", s.language));
            }

            s.self.RenderBlock(s.node, s, "code", static s =>
            {
                s.self.VisitChildren(s.node);
            });
        }, true);
    }

    protected override void VisitHardBreak(INode node)
    {
        writer.EnsureLine().WriteLine("<br>");
    }

    protected override void VisitHorizontalRule(INode node)
    {
        writer.EnsureLine().WriteLine("<hr>");
    }

    protected override void VisitBlockquote(INode node)
    {
        RenderBlock(node, (self: this, node), "blockquote",
            static s => s.self.VisitChildren(s.node));
    }

    protected override void VisitBulletList(INode node)
    {
        RenderBlock(node, (self: this, node), "ul",
            static s => s.self.VisitChildren(s.node));
    }

    protected override void VisitHeading(INode node, int level)
    {
        RenderBlock(node, (self: this, node), GetHeading(level),
            static s => s.self.VisitChildren(s.node));
    }

    protected override void VisitListItem(INode node)
    {
        RenderBlock(node, (self: this, node), "li",
            static s => s.self.VisitChildren(s.node));
    }

    protected override void VisitOrderedList(INode node)
    {
        RenderBlock(node, (self: this, node), "ol",
            static s => s.self.VisitChildren(s.node));
    }

    protected override void VisitParagraph(INode node)
    {
        RenderBlock(node, (self: this, node), "p",
            static s => s.self.VisitChildren(s.node));
    }

    protected override void VisitClassName(IMark mark, Action inner, string className)
    {
        attributes.Add(("class", $"__editor_{className}"));

        RenderInline(inner, "span",
            static a => a());
    }

    protected override void VisitBold(IMark mark, Action inner)
    {
        RenderInline(inner, "strong",
            static a => a());
    }

    protected override void VisitCode(IMark mark, Action inner)
    {
        RenderInline(inner, "code",
            static a => a());
    }

    protected override void VisitItalic(IMark mark, Action inner)
    {
        RenderInline(inner, "em",
            static a => a());
    }

    protected override void VisitUnderline(IMark mark, Action inner)
    {
        RenderInline(inner, "u",
            static a => a());
    }

    protected override void VisitLink(IMark mark, Action inner, string? href, string? target, string rel)
    {
        attributes.Add((nameof(href), href));
        attributes.Add((nameof(target), target));
        attributes.Add((nameof(rel), rel));

        RenderInline(inner, "a",
            static a => a());
    }

    protected override void VisitText(INode node)
    {
        writer.Write(node.Text ?? string.Empty);
    }

    private void RenderInline<T>(T state, string tag, Action<T> inner)
    {
        writer.Write("<");
        writer.Write(tag);
        WriteAttributes();
        writer.Write(">");
        inner(state);
        writer.Write("</");
        writer.Write(tag);
        writer.Write(">");
    }

    private void RenderBlock<T>(INode? node, T state, string tag, Action<T> inner, bool asBlock = false)
    {
        hasBlockChildren = asBlock;

        if (node != null && !asBlock)
        {
            IterateChildren(node, this, (child, self) =>
            {
                self.hasBlockChildren |= child.Type is not NodeType.Image and not NodeType.Text;
            });
        }

        writer.EnsureLine();
        writer.Write("<");
        writer.Write(tag);
        WriteAttributes();
        writer.Write(">");

        if (hasBlockChildren)
        {
            writer.PushIndent(indent);
            writer.WriteLine();
            inner(state);
            writer.EnsureLine();
            writer.PopIndent();
        }
        else
        {
            inner(state);
        }

        writer.Write("</");
        writer.Write(tag);
        writer.Write(">");
        writer.WriteLine();
    }

    private void WriteAttributes()
    {
        if (attributes.Count <= 0)
        {
            return;
        }

        foreach (var (key, value) in attributes)
        {
            if (string.IsNullOrEmpty(value))
            {
                continue;
            }

            writer.Write(" ");
            writer.Write(key);
            writer.Write("=");
            writer.Write("\"");
            writer.Write(WebUtility.HtmlEncode(value)!);
            writer.Write("\"");
        }

        attributes.Clear();
    }

    private static string GetHeading(int level)
    {
        if (level < 0 || level >= Headings.Length)
        {
            return Headings[^1];
        }
        else
        {
            return Headings[0];
        }
    }
}
