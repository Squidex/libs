// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

namespace Squidex.Text.RichText.Model;

public sealed class RichTextOptions
{
    required public HashSet<string> NodeTypes { get; init; }

    required public HashSet<string> MarkTypes { get; init; }

    public static readonly RichTextOptions Default = new RichTextOptions
    {
        NodeTypes =
        [
            NodeType.Blockquote,
            NodeType.BulletList,
            NodeType.CodeBlock,
            NodeType.Doc,
            NodeType.HardBreak,
            NodeType.HorizontalRule,
            NodeType.Image,
            NodeType.ListItem,
            NodeType.OrderedList,
            NodeType.Paragraph,
            NodeType.Heading,
            NodeType.Text,
        ],
        MarkTypes =
        [
            MarkType.Bold,
            MarkType.ClassName,
            MarkType.Code,
            MarkType.Italic,
            MarkType.Link,
            MarkType.Underline,
        ],
    };
}
