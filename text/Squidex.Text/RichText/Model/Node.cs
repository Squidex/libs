// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Squidex.Text.RichText.Model;

public sealed class Node : INode
{
    private int currentMark;

    [JsonPropertyName("type")]
    [JsonConverter(typeof(JsonStringEnumConverter<NodeType>))]
    public NodeType Type { get; set; }

    [JsonPropertyName("text")]
    public string Text { get; set; }

    [JsonPropertyName("marks")]
    public Mark[]? Marks { get; set; }

    [JsonPropertyName("content")]
    public Node[]? Content { get; set; }

    [JsonPropertyName("attrs")]
    public Attributes? Attributes { get; set; }

    public void Reset()
    {
        currentMark = 0;
    }

    public IMark? GetNextMark()
    {
        if (Marks == null || currentMark >= Marks.Length)
        {
            return null;
        }

        return Marks[currentMark++];
    }

    public void IterateContent<T>(T state, Action<INode, T, bool, bool> action)
    {
        if (Content == null)
        {
            return;
        }

        var i = 0;
        foreach (var item in Content)
        {
            var isFirst = i == 0;
            var isLast = i == Content.Length - 1;

            action(item, state, isFirst, isLast);
            i++;
        }
    }

    public int GetIntAttr(string name, int defaultValue = 0)
    {
        return Attributes?.GetIntAttr(name, defaultValue) ?? defaultValue;
    }

    public string GetStringAttr(string name, string defaultValue = "")
    {
        return Attributes?.GetStringAttr(name, defaultValue) ?? defaultValue;
    }
}
