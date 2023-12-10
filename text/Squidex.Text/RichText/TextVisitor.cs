// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System.Text;
using Squidex.Text.RichText.Model;

namespace Squidex.Text.RichText;

public sealed class TextVisitor : Visitor
{
    private readonly StringBuilder stringBuilder;
    private readonly int maxLength;

    public TextVisitor(StringBuilder stringBuilder, int maxLength)
    {
        this.stringBuilder = stringBuilder;
        this.maxLength = maxLength;
    }

    public static void Render(INode node, StringBuilder stringBuilder, int maxLength = int.MaxValue)
    {
        new TextVisitor(stringBuilder, maxLength).Visit(node);
    }

    protected override void VisitText(INode node)
    {
        if (!string.IsNullOrWhiteSpace(node.Text))
        {
            var span = node.Text.AsSpan().Trim();

            var spaceLeft = maxLength - stringBuilder.Length;
            if (spaceLeft > 0 && span.Length > spaceLeft)
            {
                span = span[..spaceLeft];
            }

            stringBuilder.Append(span);
        }
    }

    protected override void VisitHardBreak(INode node)
    {
        base.VisitHardBreak(node);
        stringBuilder.Append(' ');
    }

    protected override void VisitParagraph(INode node)
    {
        base.VisitParagraph(node);
        stringBuilder.Append(' ');
    }

    protected override void VisitChildren(INode node)
    {
        IterateChildren(node, this, static (child, self) =>
        {
            if (self.stringBuilder.Length < self.maxLength)
            {
                self.Visit(child);
            }
        });
    }
}
