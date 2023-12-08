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

    public static void Render(INode node, TextWriter textWriter)
    {
        var newRenderer = new NormalizeRenderer(textWriter);

        new MarkdownVisitor(newRenderer).Visit(node);
    }

    protected override void VisitBlockquote(INode node)
    {
        renderer.PushIndent("> ");
        VisitChildren(node);
        renderer.PopIndent();

        FinishBlock(true);
    }

    protected override void VisitBulletList(INode node)
    {
        IterateChildren(node, this, (child, self) =>
        {
            self.renderer.EnsureLine();
            self.renderer.Write('*');
            self.renderer.Write("   ");
            self.renderer.PushIndent("    ");
            self.Visit(child);
            self.renderer.PopIndent();

            if (!IsLastInContainer)
            {
                self.renderer.EnsureLine();
                self.renderer.WriteLine();
            }
        });

        renderer.EnsureLine();

        FinishBlock(true);
    }

    protected override void VisitOrderedList(INode node)
    {
        currentIndex = 0;

        IterateChildren(node, this, (child, self) =>
        {
            self.currentIndex++;
            self.renderer.EnsureLine();
            self.renderer.Write(self.currentIndex.ToString(CultureInfo.InvariantCulture));
            self.renderer.Write('.');
            self.renderer.Write("  ");
            self.renderer.PushIndent(new string(' ', IntLog10Fast(currentIndex) + 4));
            self.Visit(child);
            self.renderer.PopIndent();

            if (!IsLastInContainer)
            {
                self.renderer.EnsureLine();
                self.renderer.WriteLine();
            }
        });

        renderer.EnsureLine();

        FinishBlock(true);
    }

    protected override void VisitCodeBlock(INode node, string? language)
    {
        renderer.Write("```");
        renderer.Write(language ?? string.Empty);
        renderer.EnsureLine();
        VisitChildren(node);
        renderer.WriteLine();
        renderer.Write("```");

        FinishBlock(true);
    }

    protected override void VisitImage(INode node, string? src, string? alt, string? title)
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

    protected override void VisitHorizontalRule(INode node)
    {
        renderer.WriteLine("---");

        FinishBlock(false);
    }

    protected override void VisitHardBreak(INode node)
    {
        renderer.WriteLine();
    }

    protected override void VisitHeading(INode node, int level)
    {
        for (var i = 0; i < level; i++)
        {
            renderer.Write('#');
        }

        renderer.Write(' ');
        VisitChildren(node);

        FinishBlock(true);
    }

    protected override void VisitParagraph(INode node)
    {
        VisitChildren(node);

        FinishBlock(true);
    }

    protected override void VisitLink(IMark mark, Action inner, string? href, string? target)
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

    protected override void VisitCode(IMark mark, Action inner)
    {
        renderer.Write('`');
        inner();
        renderer.Write('`');
    }

    protected override void VisitBold(IMark mark, Action inner)
    {
        renderer.Write("**");
        inner();
        renderer.Write("**");
    }

    protected override void VisitItalic(IMark mark, Action inner)
    {
        renderer.Write('*');
        inner();
        renderer.Write('*');
    }

    protected override void VisitText(INode node)
    {
        renderer.Write(node.Text);
    }

    private void FinishBlock(bool newLine)
    {
        if (!IsLastInContainer)
        {
            renderer.FinishBlock(newLine);
        }
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
