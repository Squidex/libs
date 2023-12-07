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

        var (markdown, html) = RenderUtils.Render(source);

        var expectedMarkdown = @"
**Text1**";

        Assert.Equal(expectedMarkdown.TrimExpected(), markdown);

        var expectedHtml = @"
<strong>Text1</strong>";

        Assert.Equal(expectedHtml.TrimExpected(), html);
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

        var (markdown, html) = RenderUtils.Render(source);

        var expectedMarkdown = @"
*Text1*";

        Assert.Equal(expectedMarkdown.TrimExpected(), markdown);

        var expectedHtml = @"
<em>Text1</em>";

        Assert.Equal(expectedHtml.TrimExpected(), html);
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

        var (markdown, html) = RenderUtils.Render(source);

        var expectedMarkdown = @"
_Text1_";

        Assert.Equal(expectedMarkdown.TrimExpected(), markdown);

        var expectedHtml = @"
<u>Text1</u>";

        Assert.Equal(expectedHtml.TrimExpected(), html);
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

        var (markdown, html) = RenderUtils.Render(source);

        var expectedMarkdown = @"
`Text1`";

        Assert.Equal(expectedMarkdown.TrimExpected(), markdown);

        var expectedHtml = @"
<code>Text1</code>";

        Assert.Equal(expectedHtml.TrimExpected(), html);
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

        var (markdown, html) = RenderUtils.Render(source);

        var expectedMarkdown = @"
**_*`Text1`*_**";

        Assert.Equal(expectedMarkdown.TrimExpected(), markdown);

        var expectedHtml = @"
<strong><u><em><code>Text1</code></em></u></strong>";

        Assert.Equal(expectedHtml.TrimExpected(), html);
    }
}
