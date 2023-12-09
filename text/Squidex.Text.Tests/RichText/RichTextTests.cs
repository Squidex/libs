// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Squidex.Text.RichText.Model;
using Xunit;

namespace Squidex.Text.RichText;

public class RichTextTests
{
    [Fact]
    public void Should_render_paragraph()
    {
        var source = new Node
        {
            Type = NodeType.Paragraph,
            Content =
            [
                new Node
                {
                    Type = NodeType.Text,
                    Text = "Paragraph1"
                },
            ],
        };

        RenderUtils.AssertNode(source,
            expectedMarkdown: @"
Paragraph1",
            expectedFormattedHtml: @"
<p>Paragraph1</p>",
            expectedCompressedHtml: @"
<p>Paragraph1</p>");
    }

    [Fact]
    public void Should_render_paragraphs()
    {
        var source = new Node
        {
            Type = NodeType.Doc,
            Content =
            [
                new Node
                {
                    Type = NodeType.Paragraph,
                    Content =
                    [
                        new Node
                        {
                            Type = NodeType.Text,
                            Text = "Paragraph1"
                        },
                    ],
                },
                new Node
                {
                    Type = NodeType.Paragraph,
                    Content =
                    [
                        new Node
                        {
                            Type = NodeType.Text,
                            Text = "Paragraph2"
                        },
                    ],
                },
            ]
        };

        RenderUtils.AssertNode(source,
            expectedMarkdown: @"
Paragraph1

Paragraph2",
            expectedFormattedHtml: @"
<p>Paragraph1</p>
<p>Paragraph2</p>",
            expectedCompressedHtml: @"
<p>Paragraph1</p><p>Paragraph2</p>");
    }

    [Fact]
    public void Should_render_line_break()
    {
        var source = new Node
        {
            Type = NodeType.Paragraph,
            Content =
            [
                new Node
                {
                    Type = NodeType.Text,
                    Text = "Paragraph1"
                },
                new Node
                {
                    Type = NodeType.HardBreak
                },
                new Node
                {
                    Type = NodeType.Text,
                    Text = "Paragraph2"
                },
            ]
        };

        RenderUtils.AssertNode(source,
            expectedMarkdown: @"
Paragraph1
Paragraph2",
            expectedFormattedHtml: @"
<p>
    Paragraph1
    <br>
    Paragraph2
</p>",
            expectedCompressedHtml: @"
<p>Paragraph1<br>Paragraph2</p>");
    }

    [Fact]
    public void Should_render_horizontal_line()
    {
        var source = new Node
        {
            Type = NodeType.HorizontalRule
        };

        RenderUtils.AssertNode(source,
            expectedMarkdown: @"
---",
            expectedFormattedHtml: @"
<hr>",
            expectedCompressedHtml: @"
<hr>");
    }

    [Fact]
    public void Should_render_block_quote()
    {
        var source = new Node
        {
            Type = NodeType.Doc,
            Content =
            [
                new Node
                {
                    Type = NodeType.Blockquote,
                    Content =
                    [
                        new Node
                        {
                            Type = NodeType.Paragraph,
                            Content =
                            [
                                new Node
                                {
                                    Type = NodeType.Text,
                                    Text = "Text1"
                                },
                            ]
                        },
                    ],
                },
                new Node
                {
                    Type = NodeType.Paragraph,
                    Content =
                    [
                        new Node
                        {
                            Type = NodeType.Text,
                            Text = "Text2"
                        },
                    ]
                },
            ]
        };

        RenderUtils.AssertNode(source,
            expectedMarkdown: @"
> Text1

Text2",
            expectedFormattedHtml: @"
<blockquote>
    <p>Text1</p>
</blockquote>
<p>Text2</p>",
            expectedCompressedHtml: @"
<blockquote><p>Text1</p></blockquote><p>Text2</p>");
    }

    [Fact]
    public void Should_render_code_block()
    {
        var source = new Node
        {
            Type = NodeType.CodeBlock,
            Attributes = new Attributes
            {
                ["language"] = "html"
            },
            Content =
            [
                new Node
                {
                    Type = NodeType.Text,
                    Text = "Paragraph1"
                },
                new Node
                {
                    Type = NodeType.HardBreak
                },
                new Node
                {
                    Type = NodeType.Text,
                    Text = "Paragraph2"
                },
            ]
        };

        RenderUtils.AssertNode(source,
            expectedMarkdown: @"
```html
Paragraph1
Paragraph2
```",
            expectedFormattedHtml: @"
<pre class=""language-html"">
    <code data-code-block-language=""html"">
        Paragraph1
        <br>
        Paragraph2
    </code>
</pre>",
            expectedCompressedHtml: @"
<pre class=""language-html""><code data-code-block-language=""html"">Paragraph1<br>Paragraph2</code></pre>");
    }

    [Fact]
    public void Should_render_code_block_without_language()
    {
        var source = new Node
        {
            Type = NodeType.CodeBlock,
            Content =
            [
                new Node
                {
                    Type = NodeType.Text,
                    Text = "Paragraph1"
                },
                new Node
                {
                    Type = NodeType.HardBreak
                },
                new Node
                {
                    Type = NodeType.Text,
                    Text = "Paragraph2"
                },
            ]
        };

        RenderUtils.AssertNode(source,
            expectedMarkdown: @"
```
Paragraph1
Paragraph2
```",
            expectedFormattedHtml: @"
<pre>
    <code>
        Paragraph1
        <br>
        Paragraph2
    </code>
</pre>",
            expectedCompressedHtml: @"
<pre><code>Paragraph1<br>Paragraph2</code></pre>");
    }

    [Fact]
    public void Should_render_link()
    {
        var source = new Node
        {
            Type = NodeType.Text,
            Marks =
            [
                new Mark
                {
                    Type = MarkType.Link,
                    Attributes = new Attributes()
                    {
                        ["href"] = "https://squidex.io",
                        ["target"] = "_blank"
                    }
                },
            ],
            Text = "Link Text"
        };

        RenderUtils.AssertNode(source,
            expectedMarkdown: @"
[Link Text](https://squidex.io)",
            expectedFormattedHtml: @"
<a href=""https://squidex.io"" target=""_blank"" rel=""noopener noreferrer nofollow"">Link Text</a>",
            expectedCompressedHtml: @"
<a href=""https://squidex.io"" target=""_blank"" rel=""noopener noreferrer nofollow"">Link Text</a>");
    }

    [Fact]
    public void Should_render_empty_link()
    {
        var source = new Node
        {
            Type = NodeType.Text,
            Marks =
            [
                new Mark
                {
                    Type = MarkType.Link
                },
            ],
            Text = "Link Text"
        };

        RenderUtils.AssertNode(source,
            expectedMarkdown: @"
Link Text",
            expectedFormattedHtml: @"
<a rel=""noopener noreferrer nofollow"">Link Text</a>",
            expectedCompressedHtml: @"
<a rel=""noopener noreferrer nofollow"">Link Text</a>");
    }

    [Fact]
    public void Should_render_image()
    {
        var source = new Node
        {
            Type = NodeType.Image,
            Attributes = new Attributes()
            {
                ["src"] = "https://squidex.io/logo.png",
                ["alt"] = "Logo",
                ["title"] = "Website Logo"
            }
        };

        RenderUtils.AssertNode(source,
            expectedMarkdown: @"
![Logo](https://squidex.io/logo.png ""Website Logo"")",
            expectedFormattedHtml: @"
<img alt=""Logo"" src=""https://squidex.io/logo.png"" title=""Website Logo"">",
            expectedCompressedHtml: @"
<img alt=""Logo"" src=""https://squidex.io/logo.png"" title=""Website Logo"">");
    }

    [Fact]
    public void Should_render_blank_image()
    {
        var source = new Node
        {
            Type = NodeType.Image,
            Attributes = new Attributes()
            {
                ["src"] = "https://squidex.io/logo.png"
            }
        };

        RenderUtils.AssertNode(source,
            expectedMarkdown: @"
![](https://squidex.io/logo.png)",
            expectedFormattedHtml: @"
<img src=""https://squidex.io/logo.png"">",
            expectedCompressedHtml: @"
<img src=""https://squidex.io/logo.png"">");
    }
}
