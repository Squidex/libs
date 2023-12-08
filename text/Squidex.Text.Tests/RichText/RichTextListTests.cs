// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Squidex.Text.RichText.Model;
using Xunit;

namespace Squidex.Text.RichText;

public class RichTextListTests
{
    [Fact]
    public void Should_render_ordered_list()
    {
        var source = new Node
        {
            Type = NodeType.OrderedList,
            Content =
            [
                new Node
                {
                    Type = NodeType.ListItem,
                    Content =
                    [
                        new Node
                        {
                            Type = NodeType.Text,
                            Text = "Item1"
                        },
                    ],
                },
                new Node
                {
                    Type = NodeType.ListItem,
                    Content =
                    [
                        new Node
                        {
                            Type = NodeType.Text,
                            Text = "Item2"
                        },
                    ],
                },
            ]
        };

        var (markdown, html) = RenderUtils.Render(source);

        var expectedMarkdown = @"
1. Item1
2. Item2";

        Assert.Equal(expectedMarkdown.TrimExpected(), markdown);

        var expectedHtml = @"
<ol>
    <li>
        Item1
    </li>
    <li>
        Item2
    </li>
</ol>";

        Assert.Equal(expectedHtml.TrimExpected(), html);
    }

    [Fact]
    public void Should_render_bullet_list()
    {
        var source = new Node
        {
            Type = NodeType.BulletList,
            Content =
            [
                new Node
                {
                    Type = NodeType.ListItem,

                    Content =
                    [
                        new Node
                        {
                            Type = NodeType.Text,
                            Text = "Item1"
                        },
                    ],
                },
                new Node
                {
                    Type = NodeType.ListItem,
                    Content =
                    [
                        new Node
                        {
                            Type = NodeType.Text,
                            Text = "Item2"
                        },
                    ],
                },
            ]
        };

        var (markdown, html) = RenderUtils.Render(source);

        var expectedMarkdown = @"
* Item1
* Item2";

        Assert.Equal(expectedMarkdown.TrimExpected(), markdown);

        var expectedHtml = @"
<ul>
    <li>
        Item1
    </li>
    <li>
        Item2
    </li>
</ul>";

        Assert.Equal(expectedHtml.TrimExpected(), html);
    }

    [Fact]
    public void Should_render_nested_list()
    {
        var source = new Node
        {
            Type = NodeType.BulletList,
            Content =
            [
                new Node
                {
                    Type = NodeType.ListItem,
                    Content =
                    [
                        new Node
                        {
                            Type = NodeType.Text,
                            Text = "Item1"
                        },
                    ],
                },
                new Node
                {
                    Type = NodeType.ListItem,
                    Content =
                    [
                        new Node
                        {
                            Type = NodeType.Text,
                            Text = "Item2"
                        },
                        new Node
                        {
                            Type = NodeType.OrderedList,
                            Content =
                            [
                                new Node
                                {
                                    Type = NodeType.ListItem,
                                    Content =
                                    [
                                        new Node
                                        {
                                            Type = NodeType.Text,
                                            Text = "Item2_1"
                                        },
                                    ],
                                },
                                new Node
                                {
                                    Type = NodeType.ListItem,
                                    Content =
                                    [
                                        new Node
                                        {
                                            Type = NodeType.Text,
                                            Text = "Item2_2"
                                        },
                                    ],
                                },
                            ]
                        },
                    ],
                },
            ]
        };

        var (markdown, html) = RenderUtils.Render(source);

        var expectedMarkdown = @"
* Item1
* Item2
  1. Item2_1
  2. Item2_2";

        Assert.Equal(expectedMarkdown.TrimExpected(), markdown);

        var expectedHtml = @"
<ul>
    <li>
        Item1
    </li>
    <li>
        Item2<ol>
            <li>
                Item2_1
            </li>
            <li>
                Item2_2
            </li>
        </ol>
    </li>
</ul>";

        Assert.Equal(expectedHtml.TrimExpected(), html);
    }
}
