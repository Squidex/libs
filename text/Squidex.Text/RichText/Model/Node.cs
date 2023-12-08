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
        currentMark = 0;
    }

    public override MarkBase? GetNextMark()
    {
        if (Marks == null || currentMark >= Marks.Length)
        {
            return null;
        }

        return Marks[currentMark++];
    }

    public override void IterateContent<T>(T state, Action<NodeBase, T> action)
    {
        if (Content == null)
        {
            return;
        }

        foreach (var item in Content)
        {
            action(item, state);
        }
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
