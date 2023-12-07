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

public sealed class MarkdownVisitor2 : Visitor<bool>
{
    private readonly NormalizeRenderer renderer;

    private sealed class ListInfo
    {
        public int Index { get; set; } = 1;

        public bool IsOrdered { get; set; }
    }

    public MarkdownVisitor2(NormalizeRenderer renderer)
    {
        this.renderer = renderer;
    }

    protected override bool VisitBlockquote(Node node)
    {
        renderer.PushIndent("> ");
        VisitChildren(node);
        renderer.PopIndent();
        return true;
    }

    protected override bool VisitBulletList(Node node)
    {
        if (node.Content is not { Length: > 0 })
        {
            return true;
        }

        foreach (var child in node.Content)
        {
            renderer.EnsureLine();
            renderer.Write('*');
            renderer.Write(' ');
            renderer.PushIndent("  ");
            Visit(child);
            renderer.PopIndent();
        }

        renderer.FinishBlock(true);

        return true;
    }

    protected override bool VisitOrderedList(Node node)
    {
        if (node.Content is not { Length: > 0})
        {
            return true;
        }

        var index = 0;
        foreach (var child in node.Content)
        {
            index++;
            renderer.EnsureLine();
            renderer.Write(index.ToString(CultureInfo.InvariantCulture));
            renderer.Write('.');
            renderer.Write(' ');
            renderer.PushIndent(new string(' ', IntLog10Fast(index) + 3));
            Visit(child);
            renderer.PopIndent();
        }

        renderer.FinishBlock(true);

        return true;
    }

    protected override bool VisitCodeBlock(Node node)
    {
        var lang = node.GetString("language", string.Empty);

        renderer.Write("```");
        renderer.Write(lang);
        renderer.EnsureLine();
        VisitChildren(node);
        renderer.WriteLine();
        renderer.Write("```");
        return true;
    }

    protected override bool VisitHorizontalLine(Node node)
    {
        renderer.WriteLine("---");
        return true;
    }

    protected override bool VisitHardBreak(Node node)
    {
        renderer.WriteLine();
        return true;
    }

    protected override bool VisitHeading(Node node)
    {
        var level = node.GetNumber("level", 1);

        for (var i = 0; i < level; i++)
        {
            renderer.Write('#');
        }

        renderer.Write(' ');
        VisitChildren(node);
        renderer.FinishBlock(false);
        return true;
    }

    protected override bool VisitParagraph(Node node)
    {
        VisitChildren(node);
        renderer.FinishBlock(true);
        return true;
    }

    protected override bool VisitCode(Mark mark, Func<bool> inner)
    {
        renderer.Write('`');
        inner();
        renderer.Write('`');
        return true;
    }

    protected override bool VisitBold(Mark mark, Func<bool> inner)
    {
        renderer.Write("**");
        inner();
        renderer.Write("**");
        return true;
    }

    protected override bool VisitItalic(Mark mark, Func<bool> inner)
    {
        renderer.Write('*');
        inner();
        renderer.Write('*');
        return true;
    }

    protected override bool VisitUnderline(Mark mark, Func<bool> inner)
    {
        renderer.Write('_');
        inner();
        renderer.Write('_');
        return true;
    }

    protected override bool VisitText(Node node)
    {
        renderer.Write(node.Text ?? string.Empty);
        return true;
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
