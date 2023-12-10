// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Squidex.Text.RichText.Model;
using Xunit;

namespace Squidex.Text.RichText;

public class RichTextInlineTests
{
    [Fact]
    public void Should_render_bold()
    {
        var source = new Node
        {
            Type = NodeType.Text,
            Text = "Text1",
            Marks =
            [
                new Mark
                {
                    Type = MarkType.Bold
                },
            ]
        };

        RenderUtils.AssertNode(source,
            expectedMarkdown: @"
**Text1**",
            expectedFormattedHtml: @"
<strong>Text1</strong>",
            expectedCompressedHtml: @"
<strong>Text1</strong>");
    }

    [Fact]
    public void Should_render_italic()
    {
        var source = new Node
        {
            Type = NodeType.Text,
            Text = "Text1",
            Marks =
            [
                new Mark
                {
                    Type = MarkType.Italic
                },
            ]
        };

        RenderUtils.AssertNode(source,
            expectedMarkdown: @"
*Text1*",
            expectedFormattedHtml: @"
<em>Text1</em>",
            expectedCompressedHtml: @"
<em>Text1</em>");
    }

    [Fact]
    public void Should_render_underline()
    {
        var source = new Node
        {
            Type = NodeType.Text,
            Text = "Text1",
            Marks =
            [
                new Mark
                {
                    Type = MarkType.Underline
                },
            ]
        };

        RenderUtils.AssertNode(source,
            expectedMarkdown: @"
Text1",
            expectedFormattedHtml: @"
<u>Text1</u>",
            expectedCompressedHtml: @"
<u>Text1</u>");
    }

    [Fact]
    public void Should_render_code()
    {
        var source = new Node
        {
            Type = NodeType.Text,
            Text = "Text1",
            Marks =
            [
                new Mark
                {
                    Type = MarkType.Code
                },
            ]
        };

        RenderUtils.AssertNode(source,
            expectedMarkdown: @"
`Text1`",
            expectedFormattedHtml: @"
<code>Text1</code>",
            expectedCompressedHtml: @"
<code>Text1</code>");
    }

    [Fact]
    public void Should_render_class_name()
    {
        var source = new Node
        {
            Type = NodeType.Text,
            Text = "Text1",
            Marks =
            [
                new Mark
                {
                    Type = MarkType.ClassName,
                    Attributes = new Attributes
                    {
                        ["className"] = "text-left"
                    }
                },
            ]
        };

        RenderUtils.AssertNode(source,
            expectedMarkdown: @"
Text1",
            expectedFormattedHtml: @"
<span class=""__editor_text-left"">Text1</span>",
            expectedCompressedHtml: @"
<span class=""__editor_text-left"">Text1</span>");
    }

    [Fact]
    public void Should_render_nested()
    {
        var source = new Node
        {
            Type = NodeType.Text,
            Text = "Text1",
            Marks =
            [
                new Mark
                {
                    Type = MarkType.Bold
                },
                new Mark
                {
                    Type = MarkType.Underline
                },
                new Mark
                {
                    Type = MarkType.Italic
                },
                new Mark
                {
                    Type = MarkType.Code
                },
            ]
        };

        RenderUtils.AssertNode(source,
            expectedMarkdown: @"
***`Text1`***",
            expectedFormattedHtml: @"
<strong><u><em><code>Text1</code></em></u></strong>",
            expectedCompressedHtml: @"
<strong><u><em><code>Text1</code></em></u></strong>");
    }
}
