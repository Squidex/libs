// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Squidex.Text.RichText.Model;

namespace Squidex.Text.RichText.Json;

internal sealed class JsonNode : INode
{
    private readonly JsonMark mark = new JsonMark();
    private State currentState;

    internal struct State
    {
        public string Type;
        public JsonArray? Marks;
        public JsonObject? Attrs;
        public JsonArray? Content;
        public string? Text;
        public int MarkIndex;
    }

    public string Type
    {
        get => currentState.Type;
    }

    public string? Text
    {
        get => currentState.Text;
    }

    public bool TryUse(JsonObject source, bool recursive, RichTextOptions options)
    {
        State state = default;

        var isValid = true;
        foreach (var (key, value) in source)
        {
            switch (key)
            {
                case "type" when value is string type && options.IsSupportedNodeType(type):
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
                    isValid &= TryUse((JsonObject)content, recursive, options);
                }
            }

            if (state.Marks != null)
            {
                foreach (var markObj in state.Marks)
                {
                    isValid &= mark.TryUse((JsonObject)markObj, options);
                }
            }
        }

        return isValid;
    }

    public int GetIntAttr(string name, int defaultValue = 0)
    {
        return currentState.Attrs.GetIntAttr(name, defaultValue);
    }

    public string GetStringAttr(string name, string defaultValue = "")
    {
        return currentState.Attrs.GetStringAttr(name, defaultValue);
    }

    public IMark? GetNextMark(RichTextOptions options)
    {
        if (currentState.Marks == null || currentState.MarkIndex >= currentState.Marks.Count)
        {
            return null;
        }

        mark.TryUse((JsonObject)currentState.Marks[currentState.MarkIndex++], options);
        return mark;
    }

    public void IterateContent<T>(T state, RichTextOptions options, Action<INode, T, bool, bool> action)
    {
        var prevState = currentState;

        if (prevState.Content == null)
        {
            return;
        }

        var i = 0;
        foreach (var item in prevState.Content)
        {
            var isFirst = i == 0;
            var isLast = i == prevState.Content.Count - 1;

            TryUse((JsonObject)item, false, options);
            action(this, state, isFirst, isLast);
            i++;
        }

        currentState = prevState;
    }
}
