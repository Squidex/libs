// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Squidex.Text.RichText.Model;

namespace Squidex.RichText.Json;

internal class JsonNode : NodeBase
{
    private readonly JsonMark mark = new JsonMark();
    private State currentState;

    internal struct State
    {
        public JsonArray? Marks;
        public JsonObject? Attrs;
        public JsonArray? Content;
        public NodeType Type;
        public string? Text;
        public int MarkIndex;
    }

    public bool TryUse(JsonObject source, bool recursive)
    {
        State state = default;

        var isValid = true;
        foreach (var (key, value) in source)
        {
            switch (key)
            {
                case "type" when value.TryGetEnum<NodeType>(out var type):
                    state.Type = type;
                    break;
                case "attrs" when value is JsonObject attrs:
                    state.Attrs = attrs;
                    break;
                case "marks" when value.TryGetArrayOfObject(out var marks):
                    state.Marks = marks;
                    break;
                case "content" when value.TryGetArrayOfObject(out var content):
                    state.Content = content;
                    break;
                case "text" when value is string text:
                    state.Text = text;
                    break;
                default:
                    isValid = false;
                    break;
            }
        }

        currentState = state;

        if (recursive)
        {
            if (state.Content != null)
            {
                foreach (var content in state.Content)
                {
                    isValid &= TryUse((JsonObject)content, recursive);
                }
            }

            if (state.Marks != null)
            {
                foreach (var markObj in state.Marks)
                {
                    isValid &= mark.TryUse((JsonObject)markObj);
                }
            }
        }

        return isValid;
    }

    public override NodeType GetNodeType()
    {
        return currentState.Type;
    }

    public override string GetText()
    {
        return currentState.Text ?? string.Empty;
    }

    public override int GetIntAttr(string name, int defaultValue = 0)
    {
        if (currentState.Attrs?.TryGetValue(name, out var value) == true && value is double attr)
        {
            return (int)attr;
        }

        return defaultValue;
    }

    public override string GetStringAttr(string name, string defaultValue = "")
    {
        if (currentState.Attrs?.TryGetValue(name, out var value) == true && value is string attr)
        {
            return attr;
        }

        return defaultValue;
    }

    public override MarkBase? GetNextMark()
    {
        if (currentState.Marks == null || currentState.MarkIndex >= currentState.Marks.Count)
        {
            return null;
        }

        mark.TryUse((JsonObject)currentState.Marks[currentState.MarkIndex++]);
        return mark;
    }

    public override void IterateContent<T>(T state, Action<NodeBase, T> action)
    {
        if (currentState.Content == null)
        {
            return;
        }

        var prevState = currentState;

        foreach (var item in currentState.Content)
        {
            TryUse((JsonObject)item, false);
            action(this, state);
        }

        currentState = prevState;
    }
}
