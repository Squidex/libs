// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Squidex.Text.RichText.Model;

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
                    Type = MarkType.Bold,
                },
            ],
        };

        RenderUtils.AssertNode(source,
            markdown: @"
**Text1**",
            html: @"
<strong>Text1</strong>",
            minHtml: @"
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
                    Type = MarkType.Italic,
                },
            ],
        };

        RenderUtils.AssertNode(source,
            markdown: @"
*Text1*",
            html: @"
<em>Text1</em>",
            minHtml: @"
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
                    Type = MarkType.Underline,
                },
            ],
        };

        RenderUtils.AssertNode(source,
            markdown: @"
Text1",
            html: @"
<u>Text1</u>",
            minHtml: @"
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
                    Type = MarkType.Code,
                },
            ],
        };

        RenderUtils.AssertNode(source,
            markdown: @"
`Text1`",
            html: @"
<code>Text1</code>",
            minHtml: @"
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
                        ["className"] = "text-left",
                    },
                },
            ],
        };

        RenderUtils.AssertNode(source,
            markdown: @"
Text1",
            html: @"
<span class=""__editor_text-left"">Text1</span>",
            minHtml: @"
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
                    Type = MarkType.Bold,
                },
                new Mark
                {
                    Type = MarkType.Underline,
                },
                new Mark
                {
                    Type = MarkType.Italic,
                },
                new Mark
                {
                    Type = MarkType.Code,
                },
            ],
        };

        RenderUtils.AssertNode(source,
            markdown: @"
***`Text1`***",
            html: @"
<strong><u><em><code>Text1</code></em></u></strong>",
            minHtml: @"
<strong><u><em><code>Text1</code></em></u></strong>");
    }
}
