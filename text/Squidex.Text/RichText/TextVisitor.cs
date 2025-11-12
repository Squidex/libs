// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System.Text;
using Squidex.Text.RichText.Model;

namespace Squidex.Text.RichText;

public sealed class TextVisitor(StringBuilder stringBuilder, int maxLength, RichTextOptions options) : Visitor(options)
{
    private readonly StringBuilder stringBuilder = stringBuilder;
    private readonly int maxLength = maxLength;
    private string previousNodeType;

    public static void Render(INode node, StringBuilder stringBuilder, int maxLength = int.MaxValue, RichTextOptions? options = null)
    {
        options ??= RichTextOptions.Default;
        new TextVisitor(stringBuilder, maxLength, options).VisitRoot(node);
    }

    protected override void Visit(INode node)
    {
        base.Visit(node);
        previousNodeType = node.Type;
    }

    protected override void VisitText(INode node)
    {
        if (string.IsNullOrWhiteSpace(node.Text))
        {
            return;
        }

        if (stringBuilder.Length > 0 && previousNodeType != NodeType.Text)
        {
            stringBuilder.Append(' ');
        }

        var span = node.Text.AsSpan();

        var spaceLeft = maxLength - stringBuilder.Length;
        if (spaceLeft > 0 && span.Length > spaceLeft)
        {
            span = span[..spaceLeft];
        }

        stringBuilder.Append(span);
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
