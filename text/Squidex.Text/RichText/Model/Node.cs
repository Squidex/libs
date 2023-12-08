// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

namespace Squidex.Text.RichText.Model;

public sealed class Node : NodeBase
{
    private int currentMark;
    private int currentNode;

    public NodeType Type { get; set; }

    public string? Text { get; set; }

    public Mark[]? Marks { get; set; }

    public Node[]? Content { get; set; }

    public Attributes? Attributes { get; set; }

    public override NodeType GetNodeType()
    {
        return Type;
    }

    public override string GetText()
    {
        return Text ?? string.Empty;
    }

    public override void Reset()
    {
        currentNode = 0;
        currentMark = (Marks?.Length ?? int.MaxValue) - 1;
    }

    public override NodeBase? GetNextNode()
    {
        if (Content == null || currentNode >= Content.Length)
        {
            return null;
        }

        return Content[currentNode++];
    }

    public override MarkBase? GetNextMarkReverse()
    {
        if (Marks == null || currentMark < 0)
        {
            return null;
        }

        return Marks[currentMark--];
    }

    public override int GetIntAttr(string name, int defaultValue = 0)
    {
        if (Attributes?.TryGetValue(name, out var attr) == true && attr is int value)
        {
            return value;
        }

        return defaultValue;
    }

    public override string GetStringAttr(string name, string defaultValue = "")
    {
        if (Attributes?.TryGetValue(name, out var attr) == true && attr is string value)
        {
            return value;
        }

        return defaultValue;
    }
}
