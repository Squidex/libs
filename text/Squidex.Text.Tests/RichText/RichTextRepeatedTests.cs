// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System.Text.Json;
using Squidex.Text.RichText.Model;

namespace Squidex.Text.RichText;

public class RichTextRepeatedTests
{
    [Fact]

    public void Should_render_from_files()
    {
        var inputJson = File.ReadAllText("RichText/TestCases/Repeated/Repeated.json");
        var inputNode = JsonSerializer.Deserialize<Node>(inputJson)!;

        RenderUtils.AssertNode(inputNode,
            null,
            null,
            null,
            File.ReadAllText("RichText/TestCases/Repeated/Repeated.txt"));
    }
}
