// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System.Globalization;
using Markdig.Renderers.Normalize;
using Squidex.Text.RichText.Model;

namespace Squidex.Text.RichText;

public sealed class MarkdownVisitor : Visitor
{
    private readonly NormalizeRenderer renderer;
    private int currentIndex;

    public MarkdownVisitor(NormalizeRenderer renderer)
    {
        this.renderer = renderer;
    }

    public static void Render(NodeBase node, TextWriter textWriter)
    {
        var newRenderer = new NormalizeRenderer(textWriter);

        new MarkdownVisitor(newRenderer).Visit(node);
    }

    protected override void VisitBlockquote(NodeBase node)
    {
        renderer.PushIndent("> ");
        VisitChildren(node);
        renderer.PopIndent();
    }

    protected override void VisitBulletList(NodeBase node)
    {
        node.IterateContent(this, (node, self) =>
        {
            self.renderer.EnsureLine();
            self.renderer.Write('*');
            self.renderer.Write(' ');
            self.renderer.PushIndent("  ");
            self.Visit(node);
            self.renderer.PopIndent();
        });

        renderer.FinishBlock(true);
    }

    protected override void VisitOrderedList(NodeBase node)
    {
        currentIndex = 0;

        node.IterateContent(this, (node, self) =>
        {
            self.currentIndex++;
            self.renderer.EnsureLine();
            self.renderer.Write(self.currentIndex.ToString(CultureInfo.InvariantCulture));
            self.renderer.Write('.');
            self.renderer.Write(' ');
            self.renderer.PushIndent(new string(' ', IntLog10Fast(currentIndex) + 3));
            self.Visit(node);
            self.renderer.PopIndent();
        });

        renderer.FinishBlock(true);
    }

    protected override void VisitCodeBlock(NodeBase node, string? language)
    {
        renderer.Write("```");
        renderer.Write(language ?? string.Empty);
        renderer.EnsureLine();
        VisitChildren(node);
        renderer.WriteLine();
        renderer.Write("```");
    }

    protected override void VisitImage(NodeBase node, string? src, string? alt, string? title)
    {
        renderer.Write('!');
        renderer.Write('[');
        renderer.Write(alt ?? string.Empty);
        renderer.Write(']');
        renderer.Write('(');
        renderer.Write(src ?? string.Empty);

        if (!string.IsNullOrEmpty(title))
        {
            renderer.Write(' ');
            renderer.Write('"');
            renderer.Write(title);
            renderer.Write('"');
        }

        renderer.Write(')');
    }

    protected override void VisitHorizontalLine(NodeBase node)
    {
        renderer.WriteLine("---");
    }

    protected override void VisitHardBreak(NodeBase node)
    {
        renderer.WriteLine();
    }

    protected override void VisitHeading(NodeBase node, int level)
    {
        for (var i = 0; i < level; i++)
        {
            renderer.Write('#');
        }

        renderer.Write(' ');
        VisitChildren(node);
        renderer.FinishBlock(false);
    }

    protected override void VisitParagraph(NodeBase node)
    {
        VisitChildren(node);
        renderer.FinishBlock(true);
    }

    protected override void VisitLink(MarkBase mark, Action inner, string? href, string? target)
    {
        if (string.IsNullOrWhiteSpace(href))
        {
            inner();
            return;
        }

        renderer.Write('[');
        inner();
        renderer.Write(']');
        renderer.Write('(');
        renderer.Write(href);
        renderer.Write(')');
    }

    protected override void VisitCode(MarkBase mark, Action inner)
    {
        renderer.Write('`');
        inner();
        renderer.Write('`');
    }

    protected override void VisitBold(MarkBase mark, Action inner)
    {
        renderer.Write("**");
        inner();
        renderer.Write("**");
    }

    protected override void VisitItalic(MarkBase mark, Action inner)
    {
        renderer.Write('*');
        inner();
        renderer.Write('*');
    }

    protected override void VisitUnderline(MarkBase mark, Action inner)
    {
        renderer.Write('_');
        inner();
        renderer.Write('_');
    }

    protected override void VisitText(NodeBase node)
    {
        renderer.Write(node.GetText());
    }

    private static int IntLog10Fast(int input) =>
        (input < 10) ? 0 :
        (input < 100) ? 1 :
        (input < 1000) ? 2 :
        (input < 10000) ? 3 :
        (input < 100000) ? 4 :
        (input < 1000000) ? 5 :
        (input < 10000000) ? 6 :
        (input < 100000000) ? 7 :
        (input < 1000000000) ? 8 : 9;
}
