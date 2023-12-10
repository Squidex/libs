// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System.Globalization;
using System.Text;
using Markdig.Renderers.Normalize;
using Squidex.Text.RichText.Model;
using Squidex.Text.RichText.Writer;

namespace Squidex.Text.RichText;

public sealed class MarkdownVisitor : Visitor
{
    private readonly IWriter writer;
    private int currentIndex;

    private MarkdownVisitor(IWriter writer)
    {
        this.writer = writer;
    }

    public static void Render(INode node, StringBuilder stringBuilder)
    {
        var newWriter = new IndentedWriter(stringBuilder);

        new MarkdownVisitor(newWriter).VisitRoot(node);
    }

    protected override void VisitBlockquote(INode node)
    {
        writer.PushIndent("> ");
        VisitChildren(node);
        writer.PopIndent();

        FinishBlock(true);
    }

    protected override void VisitBulletList(INode node)
    {
        IterateChildren(node, this, static (child, self) =>
        {
            self.writer.EnsureLine();
            self.writer.Write("* ");
            self.writer.PushIndent("  ");
            self.Visit(child);
            self.writer.PopIndent();
        });

        FinishBlock(true);
    }

    protected override void VisitOrderedList(INode node)
    {
        currentIndex = 0;

        IterateChildren(node, this, static (child, self) =>
        {
            self.currentIndex++;
            self.writer.EnsureLine();
            self.writer.Write(self.currentIndex.ToString(CultureInfo.InvariantCulture));
            self.writer.Write(". ");
            self.writer.PushIndent(GetIndentByPRefix(self.currentIndex) + 3);
            self.Visit(child);
            self.writer.PopIndent();
        });

        FinishBlock(true);
    }

    protected override void VisitCodeBlock(INode node, string? language)
    {
        writer.Write("```");
        writer.Write(language ?? string.Empty);
        writer.EnsureLine();
        VisitChildren(node);
        writer.WriteLine();
        writer.Write("```");

        FinishBlock(true);
    }

    protected override void VisitImage(INode node, string? src, string? alt, string? title)
    {
        writer.Write("![");
        writer.Write(alt ?? string.Empty);
        writer.Write("](");
        writer.Write(src ?? string.Empty);

        if (!string.IsNullOrEmpty(title))
        {
            writer.Write(" \"");
            writer.Write(title);
            writer.Write("\"");
        }

        writer.Write(")");
    }

    protected override void VisitHorizontalRule(INode node)
    {
        writer.Write("---");

        FinishBlock(true);
    }

    protected override void VisitHardBreak(INode node)
    {
        writer.WriteLine();
    }

    protected override void VisitHeading(INode node, int level)
    {
        for (var i = 0; i < level; i++)
        {
            writer.Write("#");
        }

        writer.Write(" ");
        VisitChildren(node);

        FinishBlock(true);
    }

    protected override void VisitParagraph(INode node)
    {
        VisitChildren(node);

        FinishBlock(true);
    }

    protected override void VisitLink(IMark mark, Action inner, string? href, string? target, string rel)
    {
        if (string.IsNullOrWhiteSpace(href))
        {
            inner();
            return;
        }

        writer.Write("[");
        inner();
        writer.Write("](");
        writer.Write(href);
        writer.Write(")");
    }

    protected override void VisitCode(IMark mark, Action inner)
    {
        writer.Write("`");
        inner();
        writer.Write("`");
    }

    protected override void VisitBold(IMark mark, Action inner)
    {
        writer.Write("**");
        inner();
        writer.Write("**");
    }

    protected override void VisitItalic(IMark mark, Action inner)
    {
        writer.Write("*");
        inner();
        writer.Write("*");
    }

    protected override void VisitText(INode node)
    {
        writer.Write(node.Text ?? string.Empty);
    }

    private void FinishBlock(bool newLine)
    {
        if (!IsLastInContainer)
        {
            writer.WriteLine();

            if (newLine)
            {
                writer.WriteLine();
            }
        }
    }

    private static string GetIndentByPRefix(int input)
    {
        static int IntLog10Fast(int input) =>
            (input < 10) ? 0 :
            (input < 100) ? 1 :
            (input < 1000) ? 2 :
            (input < 10000) ? 3 :
            (input < 100000) ? 4 :
            (input < 1000000) ? 5 :
            (input < 10000000) ? 6 :
            (input < 100000000) ? 7 :
            (input < 1000000000) ? 8 : 9;

        return new string(' ', IntLog10Fast(input));
    }
}
