// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System.Text.Json;
using Squidex.RichText.Json;
using Squidex.Text.RichText.Model;

namespace Squidex.Text.RichText;

public class RichTextComplexTests
{
    [Fact]
    public void Should_render_complex_state()
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
                            Text = "Paragraph1",
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
                            Text = "Paragraph2",
                        },
                        new Node
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
                                            Marks =
                                            [
                                                new Mark
                                                {
                                                    Type = MarkType.Bold,
                                                },
                                            ],
                                            Text = "Item1",
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
                                            Text = "Item2",
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
                                            Text = "Item3",
                                        },
                                    ],
                                },
                            ],
                        },
                    ],
                },
            ],
        };

        RenderUtils.AssertNode(source,
            markdown: @"
Paragraph1

Paragraph2
* **Item1**
* Item2
* Item3",
            html: @"
<p>Paragraph1</p>
<p>
    Paragraph2
    <ul>
        <li><strong>Item1</strong></li>
        <li>Item2</li>
        <li>Item3</li>
    </ul>
</p>",
            minHtml: @"
<p>Paragraph1</p><p>Paragraph2<ul><li><strong>Item1</strong></li><li>Item2</li><li>Item3</li></ul></p>");
    }

    [Fact]
    public void Should_render_complex_state_from_json()
    {
        var source = new JsonObject
        {
            ["type"] = "doc",
            ["content"] = new JsonArray
            {
                new JsonObject
                {
                    ["type"] = "paragraph",
                    ["content"] = new JsonArray
                    {
                        new JsonObject
                        {
                            ["type"] = "text",
                            ["text"] = "Paragraph1",
                        },
                    },
                },
                new JsonObject
                {
                    ["type"] = "paragraph",
                    ["content"] = new JsonArray
                    {
                        new JsonObject
                        {
                            ["type"] = "text",
                            ["text"] = "Paragraph2",
                        },
                        new JsonObject
                        {
                            ["type"] = "bulletList",
                            ["content"] = new JsonArray
                            {
                                new JsonObject
                                {
                                    ["type"] = "listItem",
                                    ["content"] = new JsonArray
                                    {
                                        new JsonObject
                                        {
                                            ["type"] = "text",
                                            ["marks"] = new JsonArray
                                            {
                                                new JsonObject
                                                {
                                                    ["type"] = "bold",
                                                },
                                            },
                                            ["text"] = "Item1",
                                        },
                                    },
                                },
                                new JsonObject
                                {
                                    ["type"] = "listItem",
                                    ["content"] = new JsonArray
                                    {
                                        new JsonObject
                                        {
                                            ["type"] = "text",
                                            ["text"] = "Item2",
                                        },
                                    },
                                },
                                new JsonObject
                                {
                                    ["type"] = "listItem",
                                    ["content"] = new JsonArray
                                    {
                                        new JsonObject
                                        {
                                            ["type"] = "text",
                                            ["text"] = "Item3",
                                        },
                                    },
                                },
                            },
                        },
                    },
                },
            },
        };

        var node = new JsonNode();
        node.TryUse(source, false);

        RenderUtils.AssertNode(node,
            markdown: @"
Paragraph1

Paragraph2
* **Item1**
* Item2
* Item3",
            html: @"
<p>Paragraph1</p>
<p>
    Paragraph2
    <ul>
        <li><strong>Item1</strong></li>
        <li>Item2</li>
        <li>Item3</li>
    </ul>
</p>",
            minHtml: @"
<p>Paragraph1</p><p>Paragraph2<ul><li><strong>Item1</strong></li><li>Item2</li><li>Item3</li></ul></p>");
    }

    [Fact]
    public void Should_render_from_files()
    {
        var inputJson = File.ReadAllText("RichText/TestCases/ComplexText/ComplexText.json");
        var inputNode = JsonSerializer.Deserialize<Node>(inputJson)!;

        RenderUtils.AssertNode(inputNode,
            File.ReadAllText("RichText/TestCases/ComplexText/ComplexText.md"),
            File.ReadAllText("RichText/TestCases/ComplexText/ComplexText.html"),
            File.ReadAllText("RichText/TestCases/ComplexText/ComplexText.min.html"),
            File.ReadAllText("RichText/TestCases/ComplexText/ComplexText.txt"));
    }
}
