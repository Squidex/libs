// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

namespace Squidex.Text;

public class JsonObject : Dictionary<string, JsonValue>, IEquatable<JsonObject>
{
    public JsonObject()
    {
    }

    public JsonObject(int capacity)
        : base(capacity)
    {
    }

    public JsonObject(JsonObject source)
        : base(source)
    {
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as JsonObject);
    }

    public bool Equals(JsonObject? other)
    {
        if (other == null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        if (this.Count != other.Count)
        {
            return false;
        }

        foreach (var (key, value) in this)
        {
            if (!other.TryGetValue(key, out var otherValue) || !value.Equals(otherValue))
            {
                return false;
            }
        }

        return true;
    }

    public override int GetHashCode()
    {
        var hashCode = 17;

        foreach (var (key, value) in this.OrderBy(x => x.Key))
        {
            hashCode = (hashCode * 23) + key.GetHashCode(StringComparison.Ordinal);

            if (!Equals(value, null))
            {
                hashCode = (hashCode * 23) + value.GetHashCode();
            }
        }

        return hashCode;
    }

    public override string ToString()
    {
        return $"{{{string.Join(", ", this.Select(x => $"\"{x.Key}\":{x.Value.ToJsonString()}"))}}}";
    }

    public new JsonObject Add(string key, JsonValue value)
    {
        this[key] = value;

        return this;
    }
}
