// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

namespace Squidex.Text.RichText.Model;

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
    }

    public bool TryUse(JsonObject source, bool recursive)
    {
        State state;
        state.Attrs = null;
        state.Content = null;
        state.Marks = null;
        state.Text = null;

        var isValid = true;
        foreach (var (key, value) in source)
        {
            switch (key)
            {
                case "type" when value.TryGetEnum<NodeType>(out var type):
                    state.Type = type;
                    break;
                case "attrs" when value.Value is JsonObject attrs:
                    state.Attrs = attrs;
                    break;
                case "marks" when value.TryGetArrayOfObject(out var marks):
                    state.Marks = marks;
                    break;
                case "content" when value.TryGetArrayOfObject(out var content):
                    state.Content = content;
                    break;
                case "text" when value.Value is string text:
                    state.Text = text;
                    break;
                default:
                    isValid = false;
                    break;
            }
        }

        if (recursive)
        {
            if (state.Content != null)
            {
                foreach (var content in state.Content)
                {
                    isValid &= TryUse((JsonObject)content.Value!, recursive);
                }
            }

            if (state.Marks != null)
            {
                foreach (var markObj in state.Marks)
                {
                    isValid &= mark.TryUse((JsonObject)markObj.Value!);
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
        if (currentState.Attrs?.TryGetValue(name, out var value) == true && value.Value is double attr)
        {
            return (int)attr;
        }

        return defaultValue;
    }

    public override string GetStringAttr(string name, string defaultValue = "")
    {
        if (currentState.Attrs?.TryGetValue(name, out var value) == true && value.Value is string attr)
        {
            return attr;
        }

        return defaultValue;
    }

    public override MarkBase? GetNextMarkReverse()
    {
        throw new NotImplementedException();
    }

    public override NodeBase? GetNextNode()
    {
        throw new NotImplementedException();
    }
}
