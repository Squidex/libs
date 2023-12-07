// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Markdig.Syntax.Inlines;
using Squidex.Text.RichText.Model;

namespace Squidex.Text.RichText;

internal class MarkdownInlineVisitor : Visitor<Inline>
{
    protected override Inline VisitBold(Mark mark, Func<Inline> inner)
    {
        var result = new EmphasisInline
        {
            DelimiterCount = 1,
            DelimiterChar = '*'
        };

        return result.AppendChild(inner());
    }

    protected override Inline VisitUnderline(Mark mark, Func<Inline> inner)
    {
        var result = new EmphasisInline
        {
            DelimiterCount = 1,
            DelimiterChar = '_'
        };

        return result.AppendChild(inner());
    }

    protected override Inline VisitItalic(Mark mark, Func<Inline> inner)
    {
        var result = new EmphasisInline
        {
            DelimiterCount = 1,
            DelimiterChar = '*'
        };

        return result.AppendChild(inner());
    }

    protected override Inline VisitText(Node node)
    {
        return new LiteralInline(node.Text ?? string.Empty);
    }
}
